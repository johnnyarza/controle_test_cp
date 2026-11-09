using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels.Catalog;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Tests.ViewModels
{
    /// <summary>The Clientes / Proveedores / Materiales pages, driven like the UI does.</summary>
    public class CatalogPageTests : ServiceFixture
    {
        public CatalogPageTests(TestServer server)
            : base(server)
        {
        }

        private ClientsPageViewModel NewClientsPage(ScriptedDialogs dialogs) =>
            new ClientsPageViewModel(Services.Clients, dialogs, Session);

        private static async Task SearchAsync<T>(ListPageViewModel<T> page, string text)
            where T : class
        {
            page.SearchText = text;
            await Wait.UntilAsync(() => !page.IsLoading && page.EmptyText == null && page.Items.Count > 0 || page.EmptyText != null);
            await Task.Delay(400); // let the debounced search settle
            await Wait.UntilAsync(() => !page.IsLoading);
        }

        [Test]
        public async Task Search_shows_the_first_hundred_and_loads_more_on_request()
        {
            var tag = Unique("Lote");
            for (var i = 0; i < 105; i++)
            {
                await Services.Clients.SaveAsync(new Client { Name = $"{tag} {i:000}" });
            }

            var page = NewClientsPage(new ScriptedDialogs());
            await page.OnNavigatedToAsync();
            await SearchAsync(page, tag);

            Assert.That(page.Items, Has.Count.EqualTo(100));
            Assert.That(page.TotalCount, Is.EqualTo(105));
            Assert.That(page.HasMore, Is.True);
            Assert.That(page.StatusText, Is.EqualTo("Mostrando 100 de 105"));

            await ((AsyncRelayCommand)page.LoadMoreCommand).ExecuteAsync();
            Assert.That(page.Items, Has.Count.EqualTo(105));
            Assert.That(page.HasMore, Is.False);
            Assert.That(page.Items.Select(c => c.Name), Is.Ordered);
        }

        [Test]
        public async Task No_results_explains_itself()
        {
            var page = NewClientsPage(new ScriptedDialogs());
            await page.OnNavigatedToAsync();
            await SearchAsync(page, "zzz-no-existe-zzz");
            Assert.That(page.EmptyText, Is.EqualTo("No hay resultados para \"zzz-no-existe-zzz\"."));
        }

        [Test]
        public async Task New_client_form_shows_field_errors_then_saves_and_selects_the_new_row()
        {
            var name = Unique("Nuevo Cliente");
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var editor = (ClientEditorViewModel)dialog;
                Assert.That(editor.Title, Is.EqualTo("Nuevo cliente"));

                editor.Email = "no-es-correo";
                Assert.That(ScriptedDialogs.Save(editor), Is.False, "stays open on errors");
                Assert.That(editor.Errors["Name"], Is.EqualTo("Campo obligatorio."));
                Assert.That(editor.Errors["Email"], Is.EqualTo("Correo electrónico no válido."));

                editor.Name = name;
                editor.Email = "obra@ejemplo.com";
                return ScriptedDialogs.Save(editor);
            });

            var page = NewClientsPage(dialogs);
            page.SearchText = name;
            await ((AsyncRelayCommand)page.NewCommand).ExecuteAsync();

            Assert.That(page.SelectedItem?.Name, Is.EqualTo(name));
            Assert.That(page.SelectedItem.Email, Is.EqualTo("obra@ejemplo.com"));
        }

        [Test]
        public async Task Edit_form_loads_a_fresh_copy_and_saves_changes()
        {
            var client = await NewClientAsync();
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var editor = (ClientEditorViewModel)dialog;
                Assert.That(editor.Title, Is.EqualTo("Editar cliente"));
                Assert.That(editor.Name, Is.EqualTo(client.Name));
                editor.Phone = "999-1234";
                return ScriptedDialogs.Save(editor);
            });

            var page = NewClientsPage(dialogs);
            await SearchAsync(page, client.Name);
            page.SelectedItem = page.Items.Single();
            await ((AsyncRelayCommand)page.EditCommand).ExecuteAsync();

            Assert.That(page.SelectedItem.Phone, Is.EqualTo("999-1234"));
        }

        [Test]
        public async Task Archive_asks_first_hides_the_row_and_restore_brings_it_back()
        {
            var client = await NewClientAsync();
            var dialogs = new ScriptedDialogs();
            var page = NewClientsPage(dialogs);
            await SearchAsync(page, client.Name);
            page.SelectedItem = page.Items.Single();
            Assert.That(page.ArchiveText, Is.EqualTo("Archivar"));

            dialogs.AnswerConfirm(false);
            await ((AsyncRelayCommand)page.ToggleArchiveCommand).ExecuteAsync();
            Assert.That(page.Items, Has.Count.EqualTo(1), "cancelled: nothing happens");

            dialogs.AnswerConfirm(true);
            await ((AsyncRelayCommand)page.ToggleArchiveCommand).ExecuteAsync();
            Assert.That(page.Items, Is.Empty);
            Assert.That(((ConfirmDialogViewModel)dialogs.Shown.Last()).Text, Does.Contain("no se borra"));

            page.IncludeArchived = true;
            await Wait.UntilAsync(() => page.Items.Count == 1 && !page.IsLoading);
            page.SelectedItem = page.Items.Single();
            Assert.That(page.SelectedItem.IsArchived, Is.True);
            Assert.That(page.ArchiveText, Is.EqualTo("Restaurar"));

            dialogs.AnswerConfirm(true);
            await ((AsyncRelayCommand)page.ToggleArchiveCommand).ExecuteAsync();
            Assert.That(page.SelectedItem.IsArchived, Is.False);
        }

        [Test]
        public async Task Operators_can_edit_but_not_archive()
        {
            var op = new User { UserName = Unique("op").Replace(" ", "_"), Role = UserRoles.Operator };
            await Services.UserAdmin.CreateAsync(op, "operador123");
            var client = await NewClientAsync();

            Session.SignIn(op);
            var page = NewClientsPage(new ScriptedDialogs());
            await SearchAsync(page, client.Name);
            page.SelectedItem = page.Items.Single();

            Assert.That(page.EditCommand.CanExecute(null), Is.True);
            Assert.That(page.ToggleArchiveCommand.CanExecute(null), Is.False);
            Assert.That(page.ArchiveToolTip, Does.Contain("administrador"));
        }

        [Test]
        public async Task Material_form_offers_providers_and_no_provider()
        {
            var provider = new Provider { Name = Unique("Arenera") };
            await Services.Providers.SaveAsync(provider);
            var name = Unique("Arena fina");

            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var editor = (MaterialEditorViewModel)dialog;
                Assert.That(editor.Provider, Is.SameAs(MaterialEditorViewModel.NoProvider));
                Assert.That(editor.Providers.First(), Is.SameAs(MaterialEditorViewModel.NoProvider));

                editor.Name = name;
                editor.Provider = editor.Providers.Single(p => p.Id == provider.Id);
                return ScriptedDialogs.Save(editor);
            });

            var page = new MaterialsPageViewModel(Services.Materials, Services.Providers, dialogs, Session);
            page.SearchText = name;
            await ((AsyncRelayCommand)page.NewCommand).ExecuteAsync();

            Assert.That(page.SelectedItem.Name, Is.EqualTo(name));
            Assert.That(page.SelectedItem.ProviderName, Is.EqualTo(provider.Name));
        }
    }
}
