using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels.CompressionTests;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Tests.ViewModels
{
    /// <summary>
    /// The ensayo detail page: header, probetas grid (status colours, due count), the probeta form with its
    /// calculators, copy, filter, archive, and the administrator's unlock. "Today" is 10/03/2026.
    /// </summary>
    public class TestDetailTests : ServiceFixture
    {
        public TestDetailTests(TestServer server)
            : base(server)
        {
        }

        private TestDetailPageViewModel NewPage(CompressionTest test, ScriptedDialogs dialogs, RecordingNavigator navigator = null) =>
            new TestDetailPageViewModel(Services, dialogs, navigator ?? new RecordingNavigator(), test.Id);

        private async Task<User> SignInAsOperatorAsync()
        {
            var op = new User { UserName = Unique("op").Replace(" ", "_"), Role = UserRoles.Operator };
            await Services.UserAdmin.CreateAsync(op, "operador123");
            Session.SignIn(op);
            return op;
        }

        private async Task<Specimen> AddAsync(CompressionTest test, DateTime mold, int age, decimal? load = null)
        {
            var specimen = NewSpecimen(test.Id, mold, age, load);
            await Services.Specimens.SaveAsync(specimen, testUnlocked: true);
            return specimen;
        }

        [Test]
        public async Task Page_shows_the_header_and_probetas_with_status_and_due_count()
        {
            var test = await NewTestAsync();
            var overdue = await AddAsync(test, Today.AddDays(-10), 7);
            var dueToday = await AddAsync(test, Today.AddDays(-7), 7);
            var dueTomorrow = await AddAsync(test, Today.AddDays(-6), 7);
            var pending = await AddAsync(test, Today.AddDays(-1), 28);
            var broken = await AddAsync(test, Today.AddDays(-14), 7, load: 40);

            var navigator = new RecordingNavigator();
            var page = NewPage(test, new ScriptedDialogs(), navigator);
            await page.OnNavigatedToAsync();

            Assert.That(page.Title, Is.EqualTo($"Ensayo Nº {test.Id}"));
            Assert.That(page.Test.ProjectName, Is.EqualTo("Edificio Central"));
            Assert.That(page.TestDateText, Is.EqualTo("08/02/2026"));
            Assert.That(page.Rows.Select(r => r.Id), Is.EqualTo(new[] { overdue.Id, dueToday.Id, dueTomorrow.Id, pending.Id, broken.Id }));
            Assert.That(page.Rows.Select(r => r.RowStatus), Is.EqualTo(new[]
            {
                SpecimenStatus.Overdue, SpecimenStatus.DueSoon, SpecimenStatus.DueSoon, SpecimenStatus.Pending, SpecimenStatus.Tested,
            }));
            Assert.That(page.Rows.Select(r => r.StatusText), Is.EqualTo(new[] { "Retrasada", "Hoy", "Mañana", "Pendiente", "Rota" }));
            Assert.That(page.DueCount, Is.EqualTo(2), "overdue + today");
            Assert.That(page.DueText, Is.EqualTo("2 probetas para romper"));
            Assert.That(page.CountText, Is.EqualTo("5 probetas"));
            Assert.That(page.Rows.Last().StrengthKgPerCm2, Is.EqualTo(40 * 1000 / (Math.PI * 15 * 15 / 4)).Within(0.001));
            Assert.That(page.Rows.Last().PercentOfFck, Is.EqualTo(40 * 1000 / (Math.PI * 15 * 15 / 4) / 210 * 100).Within(0.001), "design fck 210");
            Assert.That(page.Rows.First().PercentOfFck, Is.Null, "not broken");
            Assert.That(page.DesignText, Does.EndWith(" · fck 210 kg/cm²"));

            Assert.That(page.IsUnlocked, Is.True, "administrator");
            Assert.That(page.LockText, Does.StartWith("Administrador"));
            Assert.That(page.ShowUnlock, Is.False);
            Assert.That(page.ShowLock, Is.False);

            page.BackCommand.Execute(null);
            Assert.That(navigator.BackCount, Is.EqualTo(1));
        }

        [Test]
        public async Task New_probeta_form_links_mold_date_age_and_rupture_date_and_previews_results()
        {
            var test = await NewTestAsync();
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var form = (SpecimenEditorViewModel)dialog;
                Assert.That(form.Title, Is.EqualTo("Nueva probeta"));
                Assert.That(form.MoldDate, Is.EqualTo("10/03/2026"), "today by default");
                Assert.That(form.RuptureDate, Is.Empty);

                Assert.That(ScriptedDialogs.Save(form), Is.False);
                Assert.That(form.Errors["RuptureDate"], Is.EqualTo("Campo obligatorio."));
                Assert.That(form.Errors["DiameterCm"], Is.EqualTo("Campo obligatorio."));
                Assert.That(form.Errors["HeightCm"], Is.EqualTo("Campo obligatorio."));
                Assert.That(form.Errors["WeightKg"], Is.EqualTo("Campo obligatorio."));

                form.MoldDate = "01/03/2026";
                form.AgeCommand.Execute("7");
                Assert.That(form.RuptureDate, Is.EqualTo("08/03/2026"), "mold + 7 days");
                form.RuptureDate = "29/03/2026";
                Assert.That(form.AgeDays, Is.EqualTo("28"), "the age follows the rupture date");
                form.MoldDate = "02/03/2026";
                Assert.That(form.RuptureDate, Is.EqualTo("30/03/2026"), "a new mold date keeps the age");
                form.TodayCommand.Execute(null);
                Assert.That(form.MoldDate, Is.EqualTo("10/03/2026"));
                Assert.That(form.RuptureDate, Is.EqualTo("07/04/2026"));

                Assert.That(form.DensityText, Is.EqualTo("—"));
                form.Code = "Losa nivel 2";
                form.Slump = "8,5";
                form.DiameterCm = "15";
                form.HeightCm = "30";
                form.WeightKg = "12,5";
                Assert.That(form.DensityText, Is.EqualTo("2357.85 kg/m³"), "12.5 kg / (π·0.075²·0.30 m³)");
                Assert.That(form.StrengthText, Is.EqualTo("—"), "not broken yet");
                form.RuptureLoadTon = "40";
                Assert.That(form.StrengthText, Is.EqualTo("226.35 kg/cm² (108 % fck)"));
                form.RuptureLoadTon = string.Empty;

                Assert.That(ScriptedDialogs.Save(form), Is.True);
                return true;
            });

            var page = NewPage(test, dialogs);
            await page.OnNavigatedToAsync();
            Assert.That(page.EmptyText, Is.EqualTo("Este ensayo todavía no tiene probetas. Use \"Nueva probeta\" para agregar la primera."));
            await ((AsyncRelayCommand)page.NewSpecimenCommand).ExecuteAsync();

            var row = page.SelectedRow;
            Assert.That(row?.Code, Is.EqualTo("Losa nivel 2"));
            Assert.That(row.MoldDate, Is.EqualTo(Today));
            Assert.That(row.RuptureDate, Is.EqualTo(Today.AddDays(28)));
            Assert.That(row.Slump, Is.EqualTo(8.5m));
            Assert.That(row.RuptureLoadTon, Is.Null);
            Assert.That(row.Specimen.IsLocked, Is.True, "saving locks the measurements (legacy rule)");
        }


        [Test]
        public async Task New_probeta_starts_from_the_last_one_and_shows_strength_as_percent_of_fck()
        {
            var test = await NewTestAsync();
            await AddAsync(test, Today.AddDays(-3), 7);
            var last = await AddAsync(test, Today.AddDays(-2), 28);
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var form = (SpecimenEditorViewModel)dialog;
                Assert.That(form.StartNote, Is.EqualTo($"Slump, diámetro y altura de la probeta Nº {last.Id}."));
                Assert.That(form.Slump, Is.EqualTo("8.5"));
                Assert.That(form.DiameterCm, Is.EqualTo("15"));
                Assert.That(form.HeightCm, Is.EqualTo("30"));
                Assert.That(form.WeightKg, Is.Empty, "the weight is measured per probeta");
                Assert.That(form.MoldDate, Is.EqualTo("10/03/2026"), "still today");

                form.RuptureLoadTon = "40";
                Assert.That(form.StrengthText, Is.EqualTo("226.35 kg/cm² (108 % fck)"));
                return false;
            });

            var page = NewPage(test, dialogs);
            await page.OnNavigatedToAsync();
            await ((AsyncRelayCommand)page.NewSpecimenCommand).ExecuteAsync();
        }
        [Test]
        public async Task Average_calculator_rounds_two_readings_and_rejects_bad_ones()
        {
            var test = await NewTestAsync();
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var form = (SpecimenEditorViewModel)dialog;
                form.AverageDiameterCommand.Execute(null);
                Assert.That(form.DiameterCm, Is.EqualTo("15.23"));
                form.AverageHeightCommand.Execute(null);
                Assert.That(form.HeightCm, Is.Empty, "cancelled");
                return false;
            });
            dialogs.Answer(dialog =>
            {
                var calc = (AverageCalculatorViewModel)dialog;
                Assert.That(calc.Title, Is.EqualTo("Promedio del diámetro"));
                Assert.That(calc.ResultText, Is.EqualTo("—"));
                calc.Reading1 = "0";
                calc.Reading2 = "x";
                calc.ApplyCommand.Execute(null);
                Assert.That(calc.Errors["Reading1"], Is.EqualTo("Debe ser mayor que cero."));
                Assert.That(calc.Errors["Reading2"], Is.EqualTo(NumberInput.InvalidMessage));

                calc.Reading1 = "15,2";
                calc.Reading2 = "15.25";
                Assert.That(calc.ResultText, Is.EqualTo("15.23"), "15.225 rounds up");
                var closed = false;
                calc.CloseRequested += ok => closed = ok;
                calc.ApplyCommand.Execute(null);
                Assert.That(closed, Is.True);
                return true;
            });
            dialogs.Answer(_ => false);

            var page = NewPage(test, dialogs);
            await page.OnNavigatedToAsync();
            await ((AsyncRelayCommand)page.NewSpecimenCommand).ExecuteAsync();
        }

        [Test]
        public async Task Copiar_makes_a_new_probeta_with_the_measurements_but_not_the_rupture_load()
        {
            var test = await NewTestAsync();
            var source = await AddAsync(test, Today.AddDays(-14), 7, load: 38);
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var form = (SpecimenEditorViewModel)dialog;
                Assert.That(form.Title, Is.EqualTo($"Copiar probeta Nº {source.Id}"));
                Assert.That(form.MoldDate, Is.EqualTo("24/02/2026"));
                Assert.That(form.AgeDays, Is.EqualTo("7"));
                Assert.That(form.DiameterCm, Is.EqualTo("15"));
                Assert.That(form.WeightKg, Is.EqualTo("12.5"));
                Assert.That(form.RuptureLoadTon, Is.Empty);
                form.AgeCommand.Execute("28");
                return ScriptedDialogs.Save(form);
            });

            var page = NewPage(test, dialogs);
            await page.OnNavigatedToAsync();
            page.SelectedRow = page.Rows.Single();
            await ((AsyncRelayCommand)page.CopySpecimenCommand).ExecuteAsync();

            Assert.That(page.Rows, Has.Count.EqualTo(2));
            Assert.That(page.SelectedRow.Id, Is.Not.EqualTo(source.Id));
            Assert.That(page.SelectedRow.AgeDays, Is.EqualTo(28));
            Assert.That(page.SelectedRow.RuptureLoadTon, Is.Null);
        }

        [Test]
        public async Task On_a_locked_test_an_operator_records_only_the_rupture_load_until_an_admin_unlocks_it()
        {
            var test = await NewTestAsync();
            var pending = await AddAsync(test, Today.AddDays(-7), 7);
            var overdue = await AddAsync(test, Today.AddDays(-10), 7);
            await SignInAsOperatorAsync();

            var dialogs = new ScriptedDialogs();
            var page = NewPage(test, dialogs);
            await page.OnNavigatedToAsync();
            Assert.That(page.IsUnlocked, Is.False);
            Assert.That(page.LockText, Does.StartWith("Bloqueado"));
            Assert.That(page.NewSpecimenCommand.CanExecute(null), Is.False);
            Assert.That(page.NewSpecimenToolTip, Does.Contain("bloqueado"));
            page.SelectedRow = page.Rows.First(r => r.Id == pending.Id);
            Assert.That(page.CopySpecimenCommand.CanExecute(null), Is.False);
            Assert.That(page.ToggleArchiveSpecimenCommand.CanExecute(null), Is.False);

            // Due today: only the rupture load can be typed.
            dialogs.Answer(dialog =>
            {
                var form = (SpecimenEditorViewModel)dialog;
                Assert.That(form.IsMeasurementsReadOnly, Is.True);
                Assert.That(form.IsLoadReadOnly, Is.False);
                Assert.That(form.Notice, Is.EqualTo(SpecimenEditorViewModel.RuptureLoadOnlyNotice));
                Assert.That(form.TodayCommand.CanExecute(null), Is.False);
                form.RuptureLoadTon = "41,5";
                return ScriptedDialogs.Save(form);
            });
            await ((AsyncRelayCommand)page.EditSpecimenCommand).ExecuteAsync();
            Assert.That(page.SelectedRow.RuptureLoadTon, Is.EqualTo(41.5m));
            Assert.That(page.SelectedRow.RowStatus, Is.EqualTo(SpecimenStatus.Tested));

            // Overdue: nothing can change.
            page.SelectedRow = page.Rows.First(r => r.Id == overdue.Id);
            dialogs.Answer(dialog =>
            {
                var form = (SpecimenEditorViewModel)dialog;
                Assert.That(form.CanSave, Is.False);
                Assert.That(form.CloseText, Is.EqualTo("Cerrar"));
                Assert.That(form.Notice, Does.StartWith("Probeta retrasada"));
                return false;
            });
            await ((AsyncRelayCommand)page.EditSpecimenCommand).ExecuteAsync();

            // Unlock: wrong password, then an operator's own login, then the administrator.
            dialogs.Answer(dialog =>
            {
                var unlock = (AdminUnlockViewModel)dialog;
                Assert.That(unlock.Explanation, Does.Contain($"ensayo Nº {test.Id}"));
                unlock.UserName = "admin";
                unlock.Password = "nope";
                Assert.That(ScriptedDialogs.Save(unlock), Is.False);
                Assert.That(unlock.Errors["UserName"], Is.Not.Null);
                unlock.Password = "admin";
                return ScriptedDialogs.Save(unlock);
            });
            page.UnlockCommand.Execute(null);
            Assert.That(page.IsUnlocked, Is.True);
            Assert.That(page.LockText, Does.StartWith("Desbloqueado"));
            Assert.That(page.NewSpecimenCommand.CanExecute(null), Is.True);
            Assert.That(page.ShowLock, Is.True);

            dialogs.Answer(dialog =>
            {
                var form = (SpecimenEditorViewModel)dialog;
                Assert.That(form.CanSave, Is.True);
                Assert.That(form.IsMeasurementsReadOnly, Is.False, "unlocked: everything can change");
                form.RuptureLoadTon = "39";
                return ScriptedDialogs.Save(form);
            });
            await ((AsyncRelayCommand)page.EditSpecimenCommand).ExecuteAsync();
            Assert.That(page.SelectedRow.RuptureLoadTon, Is.EqualTo(39m));

            page.LockCommand.Execute(null);
            Assert.That(page.IsUnlocked, Is.False);
            Assert.That(page.NewSpecimenCommand.CanExecute(null), Is.False);
        }

        [Test]
        public async Task An_operator_cannot_unlock_with_their_own_login()
        {
            var test = await NewTestAsync();
            var op = await SignInAsOperatorAsync();
            var unlock = new AdminUnlockViewModel(Services.Auth, Session, test.Id) { UserName = op.UserName, Password = "operador123" };
            Assert.That(ScriptedDialogs.Save(unlock), Is.False);
            Assert.That(unlock.Message, Is.EqualTo("El usuario indicado no es administrador."));
            Assert.That(Session.IsTestUnlocked(test.Id), Is.False);
        }

        [Test]
        public async Task Filter_by_mold_dates_and_numbers_then_clear()
        {
            var test = await NewTestAsync();
            var a = await AddAsync(test, new DateTime(2026, 3, 1), 28);
            var b = await AddAsync(test, new DateTime(2026, 3, 2), 28);
            var c = await AddAsync(test, new DateTime(2026, 3, 5), 28);

            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var filter = (SpecimenFilterViewModel)dialog;
                filter.MoldDateFrom = "05/03/2026";
                filter.MoldDateTo = "01/03/2026";
                filter.IdRanges = "1-";
                filter.ApplyCommand.Execute(null);
                Assert.That(filter.Errors["MoldDateTo"], Is.EqualTo("La fecha final es anterior a la inicial."));
                Assert.That(filter.Errors["IdRanges"], Is.EqualTo(IdRangeParser.InvalidMessage));

                filter.MoldDateFrom = "01/03/2026";
                filter.MoldDateTo = "2/3/26";
                filter.IdRanges = $"{a.Id}, {c.Id}";
                filter.ApplyCommand.Execute(null);
                Assert.That(filter.Result, Is.Not.Null);
                return true;
            });

            var page = NewPage(test, dialogs);
            await page.OnNavigatedToAsync();
            Assert.That(page.Rows, Has.Count.EqualTo(3));
            await ((AsyncRelayCommand)page.FilterCommand).ExecuteAsync();

            Assert.That(page.Rows.Select(r => r.Id), Is.EqualTo(new[] { a.Id }), "dates and numbers both apply");
            Assert.That(page.HasFilter, Is.True);
            Assert.That(page.FilterText, Is.EqualTo($"Moldeo 01/03/2026 – 02/03/2026 · Probetas {a.Id}, {c.Id}"));

            await ((AsyncRelayCommand)page.ClearFilterCommand).ExecuteAsync();
            Assert.That(page.Rows, Has.Count.EqualTo(3));
            Assert.That(page.HasFilter, Is.False);
            Assert.That(b.Id, Is.GreaterThan(a.Id));
        }

        [Test]
        public async Task Administrator_archives_and_restores_a_probeta()
        {
            var test = await NewTestAsync();
            var specimen = await AddAsync(test, Today.AddDays(-1), 28);
            var dialogs = new ScriptedDialogs();
            var page = NewPage(test, dialogs);
            await page.OnNavigatedToAsync();
            page.SelectedRow = page.Rows.Single();
            Assert.That(page.ArchiveText, Is.EqualTo("Archivar"));

            dialogs.AnswerConfirm(true);
            await ((AsyncRelayCommand)page.ToggleArchiveSpecimenCommand).ExecuteAsync();
            Assert.That(page.Rows, Is.Empty);
            Assert.That(((ConfirmDialogViewModel)dialogs.Shown.Last()).Text, Does.StartWith($"¿Archivar la probeta Nº {specimen.Id}?"));

            page.IncludeArchived = true;
            await Wait.UntilAsync(() => !page.IsLoading && page.Rows.Count == 1);
            page.SelectedRow = page.Rows.Single();
            Assert.That(page.SelectedRow.RowStatus, Is.EqualTo(SpecimenStatus.Archived));
            Assert.That(page.ArchiveText, Is.EqualTo("Restaurar"));
            dialogs.AnswerConfirm(true);
            await ((AsyncRelayCommand)page.ToggleArchiveSpecimenCommand).ExecuteAsync();
            Assert.That(page.Rows.Single().IsArchived, Is.False);
        }

        [Test]
        public async Task Registrar_rotura_records_the_load_and_goes_on_to_the_next_probeta_due()
        {
            var test = await NewTestAsync();
            var dueToday = await AddAsync(test, Today.AddDays(-7), 7);
            var overdue = await AddAsync(test, Today.AddDays(-10), 7);
            var pending = await AddAsync(test, Today.AddDays(-1), 28);

            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var entry = (RuptureEntryViewModel)dialog;
                Assert.That(entry.Title, Is.EqualTo($"Registrar rotura · Probeta Nº {dueToday.Id}"));
                Assert.That(entry.StatusText, Is.EqualTo("Romper hoy"));
                Assert.That(entry.AgeText, Is.EqualTo("7 días (hoy)"));
                Assert.That(entry.MoldDateText, Is.EqualTo("03/03/2026"));
                Assert.That(entry.LockNote, Is.Null, "administrator");
                Assert.That(entry.NextText, Is.EqualTo($"Guardar y siguiente (Nº {overdue.Id})"));

                Assert.That(ScriptedDialogs.Save(entry), Is.False);
                Assert.That(entry.Errors["RuptureLoadTon"], Is.EqualTo("Ingrese la rotura (mayor que cero)."));

                entry.RuptureLoadTon = "39,2";
                Assert.That(entry.StrengthText, Is.EqualTo("221.83"));
                Assert.That(entry.PercentText, Is.EqualTo("106 %"));
                Assert.That(entry.PercentLabel, Is.EqualTo("% del fck (210)"));
                var closed = false;
                entry.CloseRequested += ok => closed = ok;
                ((AsyncRelayCommand)entry.SaveAndNextCommand).ExecuteAsync().GetAwaiter().GetResult();
                Assert.That(closed, Is.True);
                Assert.That(entry.NextRequested, Is.True);
                return true;
            });
            dialogs.Answer(dialog =>
            {
                var entry = (RuptureEntryViewModel)dialog;
                Assert.That(entry.Title, Is.EqualTo($"Registrar rotura · Probeta Nº {overdue.Id}"), "the next one due");
                Assert.That(entry.StatusText, Is.EqualTo("Retrasada"));
                Assert.That(entry.HasNext, Is.False, "the pending one is not due yet");
                entry.RuptureLoadTon = "36";
                return ScriptedDialogs.Save(entry);
            });

            var page = NewPage(test, dialogs);
            await page.OnNavigatedToAsync();
            Assert.That(page.TestedCount, Is.Zero);
            Assert.That(page.AverageStrengthText, Is.EqualTo("—"));
            page.SelectedRow = page.Rows.First(r => r.Id == dueToday.Id);
            Assert.That(page.RecordRuptureCommand.CanExecute(null), Is.True);
            await ((AsyncRelayCommand)page.RecordRuptureCommand).ExecuteAsync();

            Assert.That(page.Rows.First(r => r.Id == dueToday.Id).RuptureLoadTon, Is.EqualTo(39.2m));
            Assert.That(page.Rows.First(r => r.Id == overdue.Id).RuptureLoadTon, Is.EqualTo(36m));
            Assert.That(page.Rows.First(r => r.Id == pending.Id).RuptureLoadTon, Is.Null);
            Assert.That(page.DueCount, Is.Zero);
            Assert.That(page.ActiveCount, Is.EqualTo(3));
            Assert.That(page.TestedCount, Is.EqualTo(2));
            Assert.That(page.TestedText, Is.EqualTo("de 3"));
            Assert.That(page.AverageStrengthText, Is.EqualTo("212.77"), "(221.83 + 203.72) / 2");
            Assert.That(page.AveragePercentText, Is.EqualTo("101 % fck"));

            page.SelectedRow = page.Rows.First(r => r.Id == dueToday.Id);
            Assert.That(page.RecordRuptureCommand.CanExecute(null), Is.False, "already broken");
        }

        [Test]
        public async Task Double_click_records_the_rupture_of_an_unbroken_probeta_and_opens_the_form_otherwise()
        {
            var test = await NewTestAsync();
            var pending = await AddAsync(test, Today.AddDays(-1), 28);
            var broken = await AddAsync(test, Today.AddDays(-14), 7, load: 40);
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                Assert.That(dialog, Is.TypeOf<RuptureEntryViewModel>());
                return false;
            });
            dialogs.Answer(dialog =>
            {
                Assert.That(dialog, Is.TypeOf<SpecimenEditorViewModel>());
                return false;
            });

            var page = NewPage(test, dialogs);
            await page.OnNavigatedToAsync();
            page.SelectedRow = page.Rows.First(r => r.Id == pending.Id);
            await ((AsyncRelayCommand)page.OpenSpecimenCommand).ExecuteAsync();
            page.SelectedRow = page.Rows.First(r => r.Id == broken.Id);
            await ((AsyncRelayCommand)page.OpenSpecimenCommand).ExecuteAsync();
            Assert.That(dialogs.Shown, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task On_a_locked_test_the_quick_dialog_follows_the_same_rules()
        {
            var test = await NewTestAsync();
            var dueToday = await AddAsync(test, Today.AddDays(-7), 7);
            var overdue = await AddAsync(test, Today.AddDays(-10), 7);
            await SignInAsOperatorAsync();

            var entry = new RuptureEntryViewModel(Services, test.Id, dueToday.Id, 210, null);
            await entry.InitializeAsync();
            Assert.That(entry.CanSave, Is.True);
            Assert.That(entry.LockNote, Is.EqualTo(RuptureEntryViewModel.LockedNote));
            entry.RuptureLoadTon = "38";
            Assert.That(ScriptedDialogs.Save(entry), Is.True, "an operator may record a pending rupture");

            var late = new RuptureEntryViewModel(Services, test.Id, overdue.Id, 210, null);
            await late.InitializeAsync();
            Assert.That(late.CanSave, Is.False);
            Assert.That(late.CloseText, Is.EqualTo("Cerrar"));
            Assert.That(late.Notice, Does.StartWith("Probeta retrasada"));
        }

        [Test]
        public async Task Header_is_edited_from_the_page_and_the_page_reloads()
        {
            var test = await NewTestAsync();
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                var editor = (TestEditorViewModel)dialog;
                Assert.That(editor.CanEdit, Is.True);
                editor.ProjectName = "Edificio Central - Torre B";
                return ScriptedDialogs.Save(editor);
            });

            var page = NewPage(test, dialogs);
            await page.OnNavigatedToAsync();
            await ((AsyncRelayCommand)page.EditHeaderCommand).ExecuteAsync();
            Assert.That(page.Test.ProjectName, Is.EqualTo("Edificio Central - Torre B"));
        }
    }
}
