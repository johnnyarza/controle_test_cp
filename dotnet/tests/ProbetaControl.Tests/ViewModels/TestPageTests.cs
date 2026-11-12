using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels.Catalog;
using ProbetaControl.ViewModels.CompressionTests;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Tests.ViewModels
{
    /// <summary>The Ensayos page (start page), the client search dialog and the test header form.</summary>
    public class TestPageTests : ServiceFixture
    {
        public TestPageTests(TestServer server)
            : base(server)
        {
        }

        private TestsPageViewModel NewPage(ScriptedDialogs dialogs) =>
            new TestsPageViewModel(Services, dialogs, Navigator);

        private RecordingNavigator Navigator { get; set; } = new RecordingNavigator();

        private TestEditorViewModel NewEditor(ScriptedDialogs dialogs, int? id = null) =>
            new TestEditorViewModel(Services.Tests, Services.Clients, Services.Designs, Session, dialogs, id);

        private async Task<CompressionTest> NewTestAsync(Client client, string project, int daysAgo)
        {
            var design = await NewDesignAsync();
            var test = new CompressionTest
            {
                ClientId = client.Id,
                ConcreteProviderId = client.Id,
                ConcreteDesignId = design.Id,
                ProjectName = project,
                Address = "Calle 1",
                TestDate = Today.AddDays(-daysAgo),
            };
            await Services.Tests.SaveAsync(test, testUnlocked: true);
            return test;
        }

        private static async Task LoadedAsync(TestsPageViewModel page, int count) =>
            await Wait.UntilAsync(() => !page.IsLoading && page.Items.Count == count);

        /// <summary>Answers a client search dialog: types <paramref name="search"/> and chooses the only match.</summary>
        private static void AnswerPicker(ScriptedDialogs dialogs, string search, string expectedTitle = null)
        {
            dialogs.Answer(dialog =>
            {
                var picker = (ClientPickerViewModel)dialog;
                if (expectedTitle != null)
                {
                    Assert.That(picker.Title, Is.EqualTo(expectedTitle));
                }

                picker.SearchDelayMs = 0;
                picker.SearchText = search;
                picker.SearchNowAsync().GetAwaiter().GetResult();
                Assert.That(picker.Items, Has.Count.EqualTo(1));
                Assert.That(picker.SelectedItem, Is.SameAs(picker.Items[0]), "a single match is selected");
                Assert.That(picker.ChooseCommand.CanExecute(null), Is.True);
                return true;
            });
        }

        [Test]
        public async Task List_is_newest_first_with_specimen_and_due_counts_and_the_due_filter()
        {
            var client = await NewClientAsync();
            var older = await NewTestAsync(client, "Edificio Norte", daysAgo: 30);
            var newer = await NewTestAsync(client, "Edificio Sur", daysAgo: 5);

            // Newer test: one specimen due today, one due later, one already broken.
            await Services.Specimens.SaveAsync(NewSpecimen(newer.Id, Today.AddDays(-7), 7), testUnlocked: true);
            await Services.Specimens.SaveAsync(NewSpecimen(newer.Id, Today.AddDays(-5), 28), testUnlocked: true);
            await Services.Specimens.SaveAsync(NewSpecimen(newer.Id, Today.AddDays(-10), 7, load: 25), testUnlocked: true);

            var page = NewPage(new ScriptedDialogs());
            page.SearchText = client.Name;
            await LoadedAsync(page, 2);

            Assert.That(page.Title, Is.EqualTo("Ensayos"));
            Assert.That(page.Items.Select(t => t.Id), Is.EqualTo(new[] { newer.Id, older.Id }), "newest first");
            Assert.That(page.Items[0].SpecimenCount, Is.EqualTo(3));
            Assert.That(page.Items[0].DueCount, Is.EqualTo(1));
            Assert.That(page.Items[1].DueCount, Is.Zero);

            Assert.That(page.EditText, Is.EqualTo("Abrir"));
            page.SelectedItem = page.Items[1];
            await ((AsyncRelayCommand)page.EditCommand).ExecuteAsync();
            Assert.That(((TestDetailPageViewModel)Navigator.Opened.Last()).TestId, Is.EqualTo(older.Id), "Abrir opens the ensayo");
            Assert.That(page.DueTestCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(page.OnlyDueText, Does.StartWith("Solo con probetas para romper ("));

            page.OnlyDue = true;
            await LoadedAsync(page, 1);
            Assert.That(page.Items.Single().Id, Is.EqualTo(newer.Id));

            page.SearchText = "nada coincide " + client.Name;
            await LoadedAsync(page, 0);
            Assert.That(page.EmptyText, Does.StartWith("No hay resultados"));
            page.SearchText = string.Empty;
            await Wait.UntilAsync(() => !page.IsLoading);
            Assert.That(page.Items.All(t => t.DueCount > 0), Is.True);
        }

        [Test]
        public async Task Client_filter_is_chosen_in_the_search_dialog_and_can_be_cleared()
        {
            var first = await NewClientAsync(Unique("Constructora"));
            var second = await NewClientAsync(Unique("Constructora"));
            var project = Unique("Torre");
            await NewTestAsync(first, project, daysAgo: 3);
            await NewTestAsync(second, project, daysAgo: 2);

            var dialogs = new ScriptedDialogs();
            var page = NewPage(dialogs);
            page.SearchText = project;
            await LoadedAsync(page, 2);
            Assert.That(page.ClientFilterText, Is.EqualTo("Todos los clientes"));
            Assert.That(page.ClearClientFilterCommand.CanExecute(null), Is.False);

            AnswerPicker(dialogs, first.Name, "Filtrar por cliente");
            page.ChooseClientFilterCommand.Execute(null);
            await LoadedAsync(page, 1);
            Assert.That(page.ClientFilterText, Is.EqualTo(first.Name));
            Assert.That(page.Items.Single().ClientId, Is.EqualTo(first.Id));

            page.SearchText = "nada coincide";
            await LoadedAsync(page, 0);
            page.SearchText = project;
            page.OnlyDue = true;
            await LoadedAsync(page, 0);
            Assert.That(page.EmptyText, Is.EqualTo("No hay resultados para \"" + project + "\"."));

            page.OnlyDue = false;
            page.ClearClientFilterCommand.Execute(null);
            await LoadedAsync(page, 2);
            Assert.That(page.HasClientFilter, Is.False);
        }

        [Test]
        public async Task Client_search_dialog_pages_selects_the_current_client_and_explains_no_matches()
        {
            var client = await NewClientAsync(Unique("Hormigones"));
            var picker = new ClientPickerViewModel(Services.Clients, new ScriptedDialogs(), currentId: client.Id) { SearchDelayMs = 0 };
            await picker.InitializeAsync();
            Assert.That(picker.Title, Is.EqualTo("Buscar cliente"));
            Assert.That(picker.Items, Is.Not.Empty);

            picker.SearchText = client.Name;
            await picker.SearchNowAsync();
            Assert.That(picker.SelectedItem?.Id, Is.EqualTo(client.Id));
            Assert.That(picker.Chosen, Is.SameAs(picker.SelectedItem));

            picker.SearchText = "zzz no existe";
            await picker.SearchNowAsync();
            Assert.That(picker.Items, Is.Empty);
            Assert.That(picker.EmptyText, Is.EqualTo("No hay clientes que coincidan con \"zzz no existe\"."));
            Assert.That(picker.ChooseCommand.CanExecute(null), Is.False);
        }

        [Test]
        public async Task New_test_form_validates_uses_the_search_dialogs_and_unlocks_the_test_for_its_operator()
        {
            var client = await NewClientAsync(Unique("Constructora"));
            var design = await NewDesignAsync();
            var op = new User { UserName = Unique("op").Replace(" ", "_"), Role = UserRoles.Operator };
            await Services.UserAdmin.CreateAsync(op, "operador123");
            Session.SignIn(op);

            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var editor = (TestEditorViewModel)dialog;
                Assert.That(editor.Title, Is.EqualTo("Nuevo ensayo"));
                Assert.That(editor.TestDate, Is.EqualTo("10/03/2026"), "today by default");
                Assert.That(editor.Designs.Select(d => d.Id), Does.Contain(design.Id));
                Assert.That(editor.UseClientAsProviderCommand.CanExecute(null), Is.False, "no client yet");

                editor.TestDate = "31/02/2026";
                Assert.That(ScriptedDialogs.Save(editor), Is.False);
                Assert.That(editor.Errors["TestDate"], Is.EqualTo(DateInput.InvalidMessage));
                Assert.That(editor.Errors["ClientName"], Is.EqualTo("Seleccione el cliente."));
                Assert.That(editor.Errors["ConcreteProviderName"], Is.EqualTo("Seleccione el proveedor del hormigón."));
                Assert.That(editor.Errors["ConcreteDesignId"], Is.EqualTo("Seleccione la dosificación."));
                Assert.That(editor.Errors["ProjectName"], Is.EqualTo("Campo obligatorio."));
                Assert.That(editor.Errors["Address"], Is.EqualTo("Campo obligatorio."));

                AnswerPicker(dialogs, client.Name, "Elegir cliente");
                editor.ChooseClientCommand.Execute(null);
                Assert.That(editor.ClientName, Is.EqualTo(client.Name));
                Assert.That(editor.Errors["ClientName"], Is.Null, "picking clears the error");

                editor.UseClientAsProviderCommand.Execute(null);
                Assert.That(editor.ConcreteProviderName, Is.EqualTo(client.Name));
                Assert.That(editor.UseClientAsProviderCommand.CanExecute(null), Is.False);

                editor.TestDate = "8-3-26";
                editor.ConcreteDesignId = design.Id;
                editor.ProjectName = "Edificio Mirador";
                editor.Address = "Av. Siempre Viva 742";
                Assert.That(ScriptedDialogs.Save(editor), Is.True);
                return true;
            });

            var page = NewPage(dialogs);
            page.SearchText = client.Name;
            await ((AsyncRelayCommand)page.NewCommand).ExecuteAsync();

            var created = page.SelectedItem;
            Assert.That(created?.ProjectName, Is.EqualTo("Edificio Mirador"));
            Assert.That(created.TestDate, Is.EqualTo(Today.AddDays(-2)), "8-3-26 is 8 March 2026");
            var opened = (TestDetailPageViewModel)Navigator.Opened.Last();
            Assert.That(opened.TestId, Is.EqualTo(created.Id), "a new ensayo opens at once, ready for its probetas");
            Assert.That(Session.IsTestUnlocked(created.Id), Is.True, "its creator may keep editing it");

            var stored = await Services.Tests.GetAsync(created.Id);
            Assert.That(stored.ConcreteProviderId, Is.EqualTo(client.Id));
            Assert.That(stored.ConcreteDesignId, Is.EqualTo(design.Id));

            var editor2 = NewEditor(new ScriptedDialogs(), created.Id);
            await editor2.InitializeAsync();
            Assert.That(editor2.Title, Is.EqualTo($"Ensayo Nº {created.Id}"));
            Assert.That(editor2.CanEdit, Is.True);
            editor2.Address = "Av. Siempre Viva 744";
            Assert.That(ScriptedDialogs.Save(editor2), Is.True);
        }

        [Test]
        public async Task A_new_client_can_be_created_from_the_search_dialog_of_the_test_form()
        {
            var name = Unique("Cliente nuevo");
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var picker = (ClientPickerViewModel)dialog;
                Assert.That(picker.CanCreate, Is.True);
                picker.SearchDelayMs = 0;
                picker.SearchText = name;
                picker.SearchNowAsync().GetAwaiter().GetResult();
                Assert.That(picker.Items, Is.Empty);
                Assert.That(picker.EmptyText, Does.EndWith("Si es un cliente nuevo, use \"Nuevo cliente\"."));

                ((AsyncRelayCommand)picker.NewClientCommand).ExecuteAsync().GetAwaiter().GetResult();
                Assert.That(picker.Chosen?.Name, Is.EqualTo(name), "the new client is chosen at once");
                return true;
            });
            dialogs.Answer(dialog =>
            {
                var form = (ClientEditorViewModel)dialog;
                Assert.That(form.Name, Is.EqualTo(name), "name taken from the search");
                form.Phone = "70012345";
                return ScriptedDialogs.Save(form);
            });

            var editor = NewEditor(dialogs);
            await editor.InitializeAsync();
            editor.ChooseClientCommand.Execute(null);

            Assert.That(editor.ClientName, Is.EqualTo(name));
            var stored = (await Services.Clients.SearchAsync(new ListQuery { Text = name })).Items.Single();
            Assert.That(stored.Phone, Is.EqualTo("70012345"));

            var filterPicker = new ClientPickerViewModel(Services.Clients, dialogs, "Filtrar por cliente");
            Assert.That(filterPicker.CanCreate, Is.False, "filtering offers no new client");
            Assert.That(filterPicker.NewClientCommand.CanExecute(null), Is.False);
        }

        [Test]
        public async Task Operators_see_other_tests_read_only_and_cannot_archive()
        {
            var test = await NewTestAsync(await NewClientAsync(), Unique("Puente"), daysAgo: 1);
            var op = new User { UserName = Unique("op").Replace(" ", "_"), Role = UserRoles.Operator };
            await Services.UserAdmin.CreateAsync(op, "operador123");
            Session.SignIn(op);

            var editor = NewEditor(new ScriptedDialogs(), test.Id);
            await editor.InitializeAsync();
            Assert.That(editor.IsReadOnly, Is.True);
            Assert.That(editor.LockWarning, Is.EqualTo(TestEditorViewModel.LockedMessage));
            Assert.That(editor.CloseText, Is.EqualTo("Cerrar"));
            Assert.That(editor.ChooseClientCommand.CanExecute(null), Is.False);
            Assert.That(editor.ProjectName, Is.EqualTo(test.ProjectName));

            Assert.That(ScriptedDialogs.Save(editor), Is.False);
            Assert.That(editor.Message, Is.EqualTo(TestEditorViewModel.LockedMessage));

            var page = NewPage(new ScriptedDialogs());
            page.SearchText = test.ProjectName;
            await LoadedAsync(page, 1);
            page.SelectedItem = page.Items.Single();
            Assert.That(page.EditCommand.CanExecute(null), Is.True, "opens read-only");
            Assert.That(page.ToggleArchiveCommand.CanExecute(null), Is.False);
        }

        [Test]
        public async Task Admin_archives_and_restores_a_test_from_the_list()
        {
            var test = await NewTestAsync(await NewClientAsync(), Unique("Galpón"), daysAgo: 1);
            var dialogs = new ScriptedDialogs();
            var page = NewPage(dialogs);
            page.SearchText = test.ProjectName;
            await LoadedAsync(page, 1);
            page.SelectedItem = page.Items.Single();

            dialogs.AnswerConfirm(true);
            await ((AsyncRelayCommand)page.ToggleArchiveCommand).ExecuteAsync();
            Assert.That(page.Items, Is.Empty);
            Assert.That(((ConfirmDialogViewModel)dialogs.Shown.Last()).Text, Does.Contain($"el ensayo Nº {test.Id} ({test.ProjectName})"));

            page.IncludeArchived = true;
            await LoadedAsync(page, 1);
            page.SelectedItem = page.Items.Single();
            Assert.That(page.ArchiveText, Is.EqualTo("Restaurar"));
            dialogs.AnswerConfirm(true);
            await ((AsyncRelayCommand)page.ToggleArchiveCommand).ExecuteAsync();
            Assert.That(page.Items.Single().IsArchived, Is.False);
        }

        [Test]
        public async Task A_design_archived_later_stays_on_its_test_marked_archived()
        {
            var test = await NewTestAsync(await NewClientAsync(), Unique("Silo"), daysAgo: 1);
            await Services.Tests.SetArchivedAsync(test, true);
            var design = await Services.Designs.GetAsync(test.ConcreteDesignId);
            await Services.Designs.SetArchivedAsync(design, true);
            test = await Services.Tests.GetAsync(test.Id);
            await Services.Tests.SetArchivedAsync(test, false);

            var editor = NewEditor(new ScriptedDialogs(), test.Id);
            await editor.InitializeAsync();
            var shown = editor.Designs.Single(d => d.Id == design.Id);
            Assert.That(shown.Description, Is.EqualTo(design.Description + " (archivada)"));
            Assert.That(editor.ConcreteDesignId, Is.EqualTo(design.Id));
            Assert.That(ScriptedDialogs.Save(editor), Is.True, "still saves with it");
        }
    }
}
