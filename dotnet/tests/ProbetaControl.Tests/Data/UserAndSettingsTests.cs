using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;

namespace ProbetaControl.Tests.Data
{
    /// <summary>Login, user administration, shared report images and the startup check.</summary>
    public class UserAndSettingsTests : ServiceFixture
    {
        public UserAndSettingsTests(TestServer server)
            : base(server)
        {
        }

        private async Task<User> NewOperatorAsync(string password = "operador123")
        {
            var user = new User { UserName = Unique("op").Replace(" ", "_"), FullName = "Operador Prueba", Role = UserRoles.Operator };
            await Services.UserAdmin.CreateAsync(user, password);
            return user;
        }

        [Test]
        public async Task Seeded_admin_can_log_in_and_must_change_the_password()
        {
            var session = new AppSession("TEST-PC");
            var auth = new AppServices(Database.AppConnectionSettings, session, Clock).Auth;

            var user = await auth.LoginAsync("admin", "admin");

            Assert.That(session.CurrentUser, Is.SameAs(user));
            Assert.That(session.IsAdmin, Is.True);
            Assert.That(user.MustChangePassword, Is.True);
        }

        [Test]
        public void Wrong_password_and_unknown_user_give_the_same_message()
        {
            var wrong = Assert.ThrowsAsync<ValidationException>(() => Services.Auth.LoginAsync("admin", "nope"));
            var unknown = Assert.ThrowsAsync<ValidationException>(() => Services.Auth.LoginAsync("nadie", "nope"));
            Assert.That(wrong.Message, Is.EqualTo(unknown.Message));
        }

        [Test]
        public async Task New_user_must_change_password_then_can_change_it()
        {
            var user = await NewOperatorAsync("temporal123");

            var session = new AppSession("TEST-PC");
            var services = new AppServices(Database.AppConnectionSettings, session, Clock);
            var loggedIn = await services.Auth.LoginAsync(user.UserName, "temporal123");
            Assert.That(loggedIn.MustChangePassword, Is.True);
            Assert.That(loggedIn.IsAdmin, Is.False);

            var tooShort = Assert.ThrowsAsync<ValidationException>(() => services.Auth.ChangeOwnPasswordAsync("temporal123", "corta"));
            Assert.That(tooShort.Errors.Keys, Does.Contain("NewPassword"));

            await services.Auth.ChangeOwnPasswordAsync("temporal123", "definitiva456");
            var again = await services.Auth.LoginAsync(user.UserName, "definitiva456");
            Assert.That(again.MustChangePassword, Is.False);

            var audit = await AuditRowsAsync("Users", user.Id);
            Assert.That(audit.Select(a => a.Changes), Has.None.Contain("$2"), "password hashes never reach the audit log");
        }

        [Test]
        public async Task Archived_user_cannot_log_in()
        {
            var user = await NewOperatorAsync("archivado123");
            await Services.UserAdmin.SetArchivedAsync(user, true);
            Assert.ThrowsAsync<ValidationException>(() =>
                new AppServices(Database.AppConnectionSettings, new AppSession(), Clock).Auth.LoginAsync(user.UserName, "archivado123"));
        }

        [Test]
        public async Task Only_admins_manage_users_and_the_last_admin_is_protected()
        {
            var op = await NewOperatorAsync();
            Session.SignIn(op);
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.UserAdmin.ListAsync(false));

            Session.SignIn(Admin);
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.UserAdmin.SetArchivedAsync(Admin, true), "own user");

            var admin = await Services.Users.GetAsync(Admin.Id);
            admin.Role = UserRoles.Operator;
            var self = Assert.ThrowsAsync<AccessDeniedException>(() => Services.UserAdmin.UpdateAsync(admin));
            Assert.That(self.Message, Is.EqualTo("No puede cambiar su propio rol."));

            // The last active administrator can't be demoted, even from the stale session of an archived admin.
            var second = new User { UserName = Unique("adm").Replace(" ", "_"), Role = UserRoles.Admin };
            await Services.UserAdmin.CreateAsync(second, "segundo123");
            Session.SignIn(second);
            await Services.UserAdmin.SetArchivedAsync(await Services.Users.GetAsync(Admin.Id), true);
            try
            {
                Session.SignIn(Admin);
                var lastAdmin = await Services.Users.GetAsync(second.Id);
                lastAdmin.Role = UserRoles.Operator;
                var last = Assert.ThrowsAsync<AccessDeniedException>(() => Services.UserAdmin.UpdateAsync(lastAdmin));
                Assert.That(last.Message, Does.Contain("al menos un administrador"));
            }
            finally
            {
                // Put "admin" back as the only active administrator for the other tests.
                Session.SignIn(second);
                await Services.UserAdmin.SetArchivedAsync(await Services.Users.GetAsync(Admin.Id), false);
                Session.SignIn(Admin);
                await Services.UserAdmin.SetArchivedAsync(await Services.Users.GetAsync(second.Id), true);
            }
        }

        [Test]
        public async Task Administrator_check_for_unlocking()
        {
            var op = await NewOperatorAsync("operador789");
            Assert.That((await Services.Auth.VerifyAdministratorAsync("admin", "admin")).IsAdmin, Is.True);
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Auth.VerifyAdministratorAsync(op.UserName, "operador789"));
        }

        [Test]
        public async Task Report_images_are_shared_through_the_database()
        {
            var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 };
            await Services.ReportImages.SetLogoAsync(png, "image/png");

            var otherPc = new AppServices(Database.AppConnectionSettings, new AppSession("OTRO-PC"), Clock);
            var logo = await otherPc.ReportImages.GetLogoAsync();
            Assert.That(logo.Bytes, Is.EqualTo(png));
            Assert.That(logo.ContentType, Is.EqualTo("image/png"));
            Assert.That(await otherPc.ReportImages.GetStampAsync(), Is.Null);

            var gif = Assert.ThrowsAsync<ValidationException>(() => Services.ReportImages.SetStampAsync(png, "image/gif"));
            Assert.That(gif.Message, Does.Contain("PNG o JPG"));

            var op = await NewOperatorAsync();
            Session.SignIn(op);
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.ReportImages.SetLogoAsync(png, "image/png"));
        }

        [Test]
        public async Task Startup_check_is_ready_with_the_app_account()
        {
            var result = await StartupChecker.CheckDatabaseAsync(Database.AppConnectionSettings);
            Assert.That(result.Status, Is.EqualTo(StartupStatus.Ready), result.Message);
        }

        [Test]
        public async Task Startup_check_explains_a_wrong_password_in_spanish()
        {
            var settings = Database.AppConnectionSettings;
            settings.Password = "incorrecta";
            var result = await StartupChecker.CheckDatabaseAsync(settings);
            Assert.That(result.Status, Is.EqualTo(StartupStatus.CannotConnect));
            Assert.That(result.Message, Does.Contain("contraseña").And.Contain("specimen_app"));
        }

        [Test]
        public async Task Startup_check_refuses_an_account_that_can_delete_or_change_the_structure()
        {
            // specimen_test has ALL PRIVILEGES on probeta_test_* through a wildcard database grant.
            var result = await StartupChecker.CheckDatabaseAsync(Database.AdminConnectionSettings);

            Assert.That(result.Status, Is.EqualTo(StartupStatus.AccountTooPowerful));
            Assert.That(result.Message, Does.Contain("DELETE").And.Contain("DROP").And.Contain("specimen_app"));
        }

        [Test]
        public async Task The_app_account_has_no_forbidden_rights()
        {
            using (var connection = await OpenAsAppAsync())
            {
                Assert.That(await StartupChecker.GetForbiddenPrivilegesAsync(connection, default), Is.Empty);
            }
        }
    }
}
