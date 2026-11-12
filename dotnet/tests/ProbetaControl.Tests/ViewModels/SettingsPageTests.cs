using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ProbetaControl.Data.Schema;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.ViewModels.Settings;

namespace ProbetaControl.Tests.ViewModels
{
    /// <summary>Configuración: sections by role, users, report images, own password, connection and "Acerca de".</summary>
    public class SettingsPageTests : ServiceFixture
    {
        private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };
        private static readonly byte[] GifBytes = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 1, 2, 3 };

        public SettingsPageTests(TestServer server)
            : base(server)
        {
        }

        private SettingsPageViewModel NewPage(ScriptedDialogs dialogs, System.Action changeConnection = null) =>
            new SettingsPageViewModel(Services, dialogs, changeConnection ?? (() => { }));

        private static Task RunAsync(System.Windows.Input.ICommand command) => ((AsyncRelayCommand)command).ExecuteAsync();

        private static string TempFile(string extension, byte[] bytes)
        {
            var path = Path.Combine(Path.GetTempPath(), "ProbetaControl.Tests", System.Guid.NewGuid().ToString("N") + extension);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
            return path;
        }

        [Test]
        public async Task Administrators_see_every_section_and_operators_only_their_own()
        {
            var admin = NewPage(new ScriptedDialogs());
            Assert.That(admin.Sections.Select(s => s.Title),
                Is.EqualTo(new[] { "Usuarios", "Logo y sello", "Conexión", "Mi contraseña", "Acerca de" }));
            Assert.That(admin.Sections.Select(s => s.Group).Distinct(),
                Is.EqualTo(new[] { SettingsPageViewModel.AdminGroup, SettingsPageViewModel.ComputerGroup, SettingsPageViewModel.AccountGroup }));
            Assert.That(admin.SelectedSection, Is.InstanceOf<UsersSectionViewModel>());

            await SignInNewOperatorAsync();
            var operatorPage = NewPage(new ScriptedDialogs());
            Assert.That(operatorPage.Sections.Select(s => s.Title), Is.EqualTo(new[] { "Conexión", "Mi contraseña", "Acerca de" }));
            Assert.That(operatorPage.SelectedSection, Is.InstanceOf<MyPasswordSectionViewModel>());
        }

        [Test]
        public async Task New_user_form_checks_the_password_then_the_user_must_change_it_at_first_login()
        {
            var userName = Unique("nuevo").Replace(" ", "_");
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var editor = (UserEditorViewModel)dialog;
                Assert.That(editor.Title, Is.EqualTo("Nuevo usuario"));
                Assert.That(editor.Role, Is.EqualTo(UserRoles.Operator), "operator by default");

                editor.UserName = userName;
                editor.FullName = "Usuario Nuevo";
                editor.Password = "corta";
                editor.ConfirmPassword = "corta";
                Assert.That(ScriptedDialogs.Save(editor), Is.False);
                Assert.That(editor.Errors["Password"], Does.Contain("al menos 8"));

                editor.Password = "inicial123";
                editor.ConfirmPassword = "inicial124";
                Assert.That(ScriptedDialogs.Save(editor), Is.False);
                Assert.That(editor.Errors["ConfirmPassword"], Is.EqualTo("Las contraseñas no coinciden."));

                editor.ConfirmPassword = "inicial123";
                return ScriptedDialogs.Save(editor);
            });

            var page = NewPage(dialogs);
            await page.OnNavigatedToAsync();
            var users = page.Find<UsersSectionViewModel>();
            await RunAsync(users.NewCommand);

            Assert.That(users.SelectedItem.UserName, Is.EqualTo(userName));
            Assert.That(users.SelectedItem.RoleText, Is.EqualTo("Operador"));
            Assert.That(users.SelectedItem.StatusText, Is.EqualTo("Debe cambiar la contraseña"));

            var login = await new AppServices(Database.AppConnectionSettings, new AppSession(), Clock).Auth.LoginAsync(userName, "inicial123");
            Assert.That(login.MustChangePassword, Is.True);
        }

        [Test]
        public async Task An_administrator_edits_others_but_not_their_own_role_archive_or_reset()
        {
            var page = NewPage(new ScriptedDialogs());
            await page.OnNavigatedToAsync();
            var users = page.Find<UsersSectionViewModel>();

            users.SelectedItem = users.Items.Single(r => r.User.Id == Admin.Id);
            Assert.That(users.SelectedItem.StatusText, Is.EqualTo("Sesión actual"));
            Assert.That(users.ToggleArchiveCommand.CanExecute(null), Is.False);
            Assert.That(users.ResetPasswordCommand.CanExecute(null), Is.False);
            Assert.That(new UserEditorViewModel(Services.UserAdmin, Session, Admin).CanChangeRole, Is.False);

            var op = await SignInNewOperatorAsync();
            SignInAsAdmin();
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var editor = (UserEditorViewModel)dialog;
                Assert.That(editor.IsNew, Is.False);
                Assert.That(editor.CanChangeRole, Is.True);
                editor.FullName = "Operador Editado";
                editor.Role = UserRoles.Admin;
                return ScriptedDialogs.Save(editor);
            });
            users = NewPage(dialogs).Find<UsersSectionViewModel>();
            await users.LoadAsync();
            users.SelectedItem = users.Items.Single(r => r.User.Id == op.Id);
            await RunAsync(users.EditCommand);

            Assert.That(users.SelectedItem.FullName, Is.EqualTo("Operador Editado"));
            Assert.That(users.SelectedItem.RoleText, Is.EqualTo("Administrador"));
        }

        [Test]
        public async Task Reset_password_gives_a_temporary_one_and_archive_stops_the_login()
        {
            var op = await SignInNewOperatorAsync();
            SignInAsAdmin();

            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var reset = (ResetPasswordViewModel)dialog;
                reset.Password = "temporal123";
                reset.ConfirmPassword = "temporal123";
                return ScriptedDialogs.Save(reset);
            });
            dialogs.AnswerConfirm(true);

            var users = NewPage(dialogs).Find<UsersSectionViewModel>();
            await users.LoadAsync();
            users.SelectedItem = users.Items.Single(r => r.User.Id == op.Id);

            await RunAsync(users.ResetPasswordCommand);
            Assert.That(users.Message, Does.Contain("restablecida"));
            var other = new AppServices(Database.AppConnectionSettings, new AppSession(), Clock);
            Assert.That((await other.Auth.LoginAsync(op.UserName, "temporal123")).MustChangePassword, Is.True);

            await RunAsync(users.ToggleArchiveCommand);
            Assert.That(users.Items.Any(r => r.User.Id == op.Id), Is.False, "archived users are hidden");
            Assert.ThrowsAsync<ValidationException>(() => other.Auth.LoginAsync(op.UserName, "temporal123"));

            users.IncludeArchived = true;
            await Wait.UntilAsync(() => users.Items.Any(r => r.User.Id == op.Id));
            Assert.That(users.Items.Single(r => r.User.Id == op.Id).StatusText, Is.EqualTo("Archivado"));
        }

        [Test]
        public async Task Report_images_are_chosen_from_a_file_checked_and_removed()
        {
            var dialogs = new ScriptedDialogs();
            var page = NewPage(dialogs);
            var images = page.Find<ReportImagesSectionViewModel>();
            await images.LoadAsync();
            Assert.That(images.Logo.HasImage, Is.False);
            Assert.That(images.Logo.ChooseText, Is.EqualTo("Elegir imagen…"));

            dialogs.AnswerFile(null);
            await RunAsync(images.Logo.ChooseCommand);
            Assert.That(images.Logo.HasImage, Is.False, "cancelled");

            dialogs.AnswerFile(TempFile(".png", GifBytes));
            await RunAsync(images.Logo.ChooseCommand);
            Assert.That(images.Logo.Errors["Image"], Is.EqualTo("Use una imagen PNG o JPG."), "a renamed GIF is refused");
            Assert.That(images.Logo.HasImage, Is.False);

            dialogs.AnswerFile(TempFile(".png", PngBytes));
            await RunAsync(images.Logo.ChooseCommand);
            Assert.That(images.Logo.Bytes, Is.EqualTo(PngBytes));
            Assert.That(images.Logo.InfoText, Is.EqualTo("PNG · 1 KB"));
            Assert.That((await Services.ReportImages.GetLogoAsync()).Bytes, Is.EqualTo(PngBytes), "saved at once");

            dialogs.AnswerConfirm(true);
            await RunAsync(images.Logo.RemoveCommand);
            Assert.That(images.Logo.HasImage, Is.False);
            Assert.That(await Services.ReportImages.GetLogoAsync(), Is.Null);
            Assert.That(images.Stamp.HasImage, Is.False, "the stamp is separate");
        }

        [Test]
        public async Task Any_user_changes_their_own_password()
        {
            var op = await SignInNewOperatorAsync();
            var section = NewPage(new ScriptedDialogs()).Find<MyPasswordSectionViewModel>();

            section.CurrentPassword = "incorrecta";
            section.NewPassword = section.ConfirmPassword = "nueva12345";
            await RunAsync(section.SaveCommand);
            Assert.That(section.Errors["CurrentPassword"], Is.EqualTo("La contraseña actual no es correcta."));

            section.CurrentPassword = "operador123";
            section.ConfirmPassword = "otra12345";
            await RunAsync(section.SaveCommand);
            Assert.That(section.Errors["ConfirmPassword"], Is.EqualTo("Las contraseñas no coinciden."));

            section.ConfirmPassword = "nueva12345";
            await RunAsync(section.SaveCommand);
            Assert.That(section.MessageKind, Is.EqualTo(MessageKind.Success));
            Assert.That(section.CurrentPassword, Is.Empty, "fields are cleared");

            var login = await new AppServices(Database.AppConnectionSettings, new AppSession(), Clock).Auth.LoginAsync(op.UserName, "nueva12345");
            Assert.That(login.MustChangePassword, Is.False);
        }

        [Test]
        public void Changing_the_connection_asks_first_then_signs_out()
        {
            var changed = 0;
            var dialogs = new ScriptedDialogs();
            var section = NewPage(dialogs, () => changed++).Find<ConnectionSectionViewModel>();

            Assert.That(section.Server, Is.EqualTo($"{Database.AppConnectionSettings.Host} : {Database.AppConnectionSettings.Port}"));
            Assert.That(section.Database, Is.EqualTo(Database.Name));
            Assert.That(section.UserName, Is.EqualTo(Database.AppConnectionSettings.UserName));

            dialogs.AnswerConfirm(false);
            section.ChangeCommand.Execute(null);
            Assert.That(changed, Is.Zero);

            dialogs.AnswerConfirm(true);
            section.ChangeCommand.Execute(null);
            Assert.That(changed, Is.EqualTo(1));
        }

        [Test]
        public async Task About_shows_the_versions_of_the_program_database_and_server()
        {
            var about = NewPage(new ScriptedDialogs()).Find<AboutSectionViewModel>();
            await about.LoadAsync();

            Assert.That(about.VersionText, Does.StartWith("Versión "));
            Assert.That(about.SchemaVersion, Is.EqualTo($"versión {SchemaInfo.RequiredVersion}"));
            Assert.That(about.ServerVersion, Does.StartWith("8."));
            Assert.That(about.MachineName, Is.EqualTo("TEST-PC"));
        }
    }
}
