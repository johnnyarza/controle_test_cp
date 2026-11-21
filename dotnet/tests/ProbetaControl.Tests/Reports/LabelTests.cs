using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels.CompressionTests;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.ViewModels.Reports;
using ProbetaControl.ViewModels.Settings;

namespace ProbetaControl.Tests.Reports
{
    /// <summary>QR labels against a real database: codes, settings, the labels printed and the screens that print them.</summary>
    public class LabelTests : ServiceFixture
    {
        public LabelTests(TestServer server)
            : base(server)
        {
        }

        private async Task SetServerAsync(string name)
        {
            Session.SignIn(Admin);
            await Services.LabelSettings.SaveAsync(new LabelSettings { ServerName = name, Sheet = LabelSheet.Default });
        }

        private async Task<Specimen> AddAsync(CompressionTest test, int age = 7, decimal? load = null)
        {
            var specimen = NewSpecimen(test.Id, Today.AddDays(-3), age, load);
            await Services.Specimens.SaveAsync(specimen, testUnlocked: true);
            return specimen;
        }

        /// <summary>
        /// Gives probetas whose labels a test reads back a code from <see cref="ReadableCodes"/> (their random ones could be
        /// one of the rare codes ZXing's image detector misses).
        /// </summary>
        private async Task UseReadableCodesAsync(params Specimen[] specimens)
        {
            using (var connection = await OpenAsAdminAsync())
            {
                foreach (var specimen in specimens)
                {
                    specimen.PublicCode = ReadableCodes.Next();
                    await connection.ExecuteAsync("UPDATE Specimens SET PublicCode = @PublicCode WHERE Id = @Id", specimen);
                }
            }
        }

        [Test]
        public async Task A_new_probeta_gets_its_own_random_code_which_never_changes()
        {
            var test = await NewTestAsync();
            var first = await AddAsync(test);
            var second = await AddAsync(test);
            Assert.That(first.PublicCode, Does.Match("^[0-9A-F]{16}$"));
            Assert.That(second.PublicCode, Is.Not.EqualTo(first.PublicCode));

            var stored = await Services.Specimens.GetAsync(first.Id);
            Assert.That(stored.PublicCode, Is.EqualTo(first.PublicCode));

            // Edited from a form that doesn't carry the code: it stays, and the audit doesn't show a change.
            stored.PublicCode = null;
            stored.WeightKg = 12.7m;
            await Services.Specimens.SaveAsync(stored, testUnlocked: true);
            Assert.That((await Services.Specimens.GetAsync(first.Id)).PublicCode, Is.EqualTo(first.PublicCode));
            var audit = await AuditRowsAsync("Specimens", first.Id);
            Assert.That(audit.Last().Changes, Does.Not.Contain("PublicCode"));
            Assert.That(audit.First().Changes, Does.Contain(first.PublicCode), "the code is recorded when the probeta is created");

            // A copy (same values, saved as new) gets a code of its own.
            var copy = stored.Clone();
            copy.Id = 0;
            copy.PublicCode = first.PublicCode;
            await Services.Specimens.SaveAsync(copy, testUnlocked: true);
            Assert.That(copy.PublicCode, Is.Not.EqualTo(first.PublicCode));
        }

        [Test]
        public async Task Codes_are_unique_in_the_database_and_script_013_makes_them_the_same_way()
        {
            using (var connection = await OpenAsAdminAsync())
            {
                var code = await connection.ExecuteScalarAsync<string>("SELECT UPPER(HEX(RANDOM_BYTES(8)))");
                Assert.That(code, Does.Match("^[0-9A-F]{16}$"), "the expression script 013 fills existing probetas with");

                var column = await connection.QuerySingleAsync<(string IsNullable, string ColumnType)>(
                    "SELECT IS_NULLABLE, COLUMN_TYPE FROM information_schema.COLUMNS " +
                    "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Specimens' AND COLUMN_NAME = 'PublicCode'");
                Assert.That(column, Is.EqualTo(("NO", "char(16)")));

                var unique = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() " +
                    "AND TABLE_NAME = 'Specimens' AND INDEX_NAME = 'UX_Specimens_PublicCode' AND NON_UNIQUE = 0");
                Assert.That(unique, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task Settings_start_without_a_server_and_only_an_administrator_changes_them()
        {
            using (var connection = await OpenAsAdminAsync())
            {
                await connection.ExecuteAsync("UPDATE AppSettings SET TextValue = NULL WHERE SettingKey = 'LabelServerName'");
                await connection.ExecuteAsync("UPDATE AppSettings SET TextValue = 'basura' WHERE SettingKey = 'LabelSheet'");
            }

            var start = await Services.LabelSettings.GetAsync();
            Assert.That(start.ServerName, Is.Null, "a new database has no server name");
            Assert.That(start.Sheet.SameAs(LabelSheet.Default), Is.True, "an unreadable sheet falls back to the default");

            var chosen = LabelSheet.Presets[3].Sheet;
            await Services.LabelSettings.SaveAsync(new LabelSettings { ServerName = " HTTP://Servidor-Lab/ ", Sheet = chosen });
            var saved = await Services.LabelSettings.GetAsync();
            Assert.That(saved.ServerName, Is.EqualTo("servidor-lab"));
            Assert.That(saved.Sheet.SameAs(chosen), Is.True);

            var ex = Assert.ThrowsAsync<ValidationException>(() => Services.LabelSettings.SaveAsync(
                new LabelSettings { ServerName = "192.168.0.10", Sheet = new LabelSheet { Columns = 3, Rows = 8, WidthMm = 80, HeightMm = 37 } }));
            Assert.That(ex.Errors.Keys, Is.EquivalentTo(new[] { "ServerName", "Sheet" }));

            await SignInNewOperatorAsync();
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.LabelSettings.SaveAsync(new LabelSettings { ServerName = "otro" }));
            Assert.That((await Services.LabelSettings.GetAsync()).ServerName, Is.EqualTo("servidor-lab"), "operators read them");
        }

        [Test]
        public async Task Labels_are_for_the_active_probetas_of_the_filter_and_each_QR_opens_its_own_page()
        {
            await SetServerAsync("servidor-lab");
            var test = await NewTestAsync();
            var a = await AddAsync(test);
            var broken = await AddAsync(test, load: 40);
            var archived = await AddAsync(test);
            await Services.Specimens.SetArchivedAsync(archived, true);
            var c = await AddAsync(test, age: 28);
            await UseReadableCodesAsync(a, broken, archived, c);

            var document = await Services.Reports.BuildLabelsAsync(test.Id, null, onlyUntested: true, startPosition: 1);
            using (var page = LabelLayoutTests.Draw(document, 1))
            {
                Assert.That(LabelLayoutTests.ReadQr(page, LabelSheet.Default, 1), Is.EqualTo("HTTP://SERVIDOR-LAB/P/" + a.PublicCode));
                Assert.That(LabelLayoutTests.ReadQr(page, LabelSheet.Default, 2), Is.EqualTo("HTTP://SERVIDOR-LAB/P/" + c.PublicCode),
                    "not the broken one, not the archived one");
                Assert.That(LabelLayoutTests.ReadQr(page, LabelSheet.Default, 3), Is.Null);
            }

            var withBroken = await Services.Reports.BuildLabelsAsync(test.Id, null, onlyUntested: false, startPosition: 3);
            using (var page = LabelLayoutTests.Draw(withBroken, 1))
            {
                Assert.That(LabelLayoutTests.ReadQr(page, LabelSheet.Default, 2), Is.Null, "before the start");
                Assert.That(LabelLayoutTests.ReadQr(page, LabelSheet.Default, 4), Is.EqualTo("HTTP://SERVIDOR-LAB/P/" + broken.PublicCode));
            }

            var filtered = await Services.Reports.BuildLabelsAsync(test.Id,
                new SpecimenFilter { IdRanges = new[] { new IdRange(c.Id, c.Id), new IdRange(archived.Id, archived.Id) }, IncludeArchived = true },
                onlyUntested: true, startPosition: 1);
            using (var page = LabelLayoutTests.Draw(filtered, 1))
            {
                Assert.That(LabelLayoutTests.ReadQr(page, LabelSheet.Default, 1), Is.EqualTo("HTTP://SERVIDOR-LAB/P/" + c.PublicCode));
                Assert.That(LabelLayoutTests.ReadQr(page, LabelSheet.Default, 2), Is.Null, "archived probetas never get labels");
            }

            var none = Assert.ThrowsAsync<ValidationException>(() => Services.Reports.BuildLabelsAsync(test.Id,
                new SpecimenFilter { IdRanges = new[] { new IdRange(broken.Id, broken.Id) } }, onlyUntested: true, startPosition: 1));
            Assert.That(none.Errors.Keys, Is.EqualTo(new[] { "Specimens" }));
        }

        [Test]
        public async Task Without_a_server_name_labels_are_not_printed()
        {
            using (var connection = await OpenAsAdminAsync())
            {
                await connection.ExecuteAsync("UPDATE AppSettings SET TextValue = NULL WHERE SettingKey = 'LabelServerName'");
            }

            var test = await NewTestAsync();
            await AddAsync(test);
            var ex = Assert.ThrowsAsync<ValidationException>(() => Services.Reports.BuildLabelsAsync(test.Id, null, true, 1));
            Assert.That(ex.Errors["ServerName"], Does.Contain("Configuración → Etiquetas"));

            var dialog = new LabelOptionsViewModel(Services.LabelSettings, new[] { NewSpecimen(test.Id, Today, 7) }, "Ensayo", single: false);
            await dialog.InitializeAsync();
            Assert.That(dialog.Message, Does.Contain("Configuración → Etiquetas"));
            Assert.That(dialog.CanPrint, Is.False);
            Assert.That(dialog.AcceptCommand.CanExecute(null), Is.False);
        }

        [Test]
        public async Task Options_dialog_shows_where_the_labels_go_and_starts_where_clicked()
        {
            await SetServerAsync("servidor-lab");
            var specimens = Enumerable.Range(0, 5).Select(i => NewSpecimen(1, Today, 7, i == 0 ? 30m : (decimal?)null)).ToList();
            var dialog = new LabelOptionsViewModel(Services.LabelSettings, specimens, "Ensayo Nº 1 · todas sus probetas", single: false);
            await dialog.InitializeAsync();

            Assert.That(dialog.SheetText, Is.EqualTo("Hoja A4 · 3 × 8 etiquetas de 70 × 37 mm"));
            Assert.That(dialog.Columns, Is.EqualTo(3));
            Assert.That(dialog.Positions, Has.Count.EqualTo(24));
            Assert.That(dialog.ShowOnlyUntested, Is.True);
            Assert.That(dialog.OnlyUntested, Is.True, "labels go on before the probetas are broken");
            Assert.That(dialog.LabelCount, Is.EqualTo(4));
            Assert.That(dialog.SummaryText, Is.EqualTo("4 etiquetas en 1 hoja, de la 1 a la 4."));

            dialog.ChooseStartCommand.Execute(dialog.Positions[21]);
            Assert.That(dialog.StartPosition, Is.EqualTo(22));
            Assert.That(dialog.SummaryText, Is.EqualTo("4 etiquetas en 2 hojas, empezando en la etiqueta 22 de la primera."));
            Assert.That(dialog.Positions.Count(p => p.State == LabelPositionState.Used), Is.EqualTo(21));
            Assert.That(dialog.Positions.Count(p => p.State == LabelPositionState.Printed), Is.EqualTo(3), "22-24 on this sheet");
            Assert.That(dialog.Positions.Single(p => p.IsStart).Number, Is.EqualTo(22));
            Assert.That(dialog.Positions[0].Description, Is.EqualTo("Etiqueta 1: ya usada. Clic para empezar aquí."));

            dialog.OnlyUntested = false;
            Assert.That(dialog.LabelCount, Is.EqualTo(5));

            dialog.StartPosition = 99;
            Assert.That(dialog.StartPosition, Is.EqualTo(24), "kept on the sheet");

            var accepted = false;
            dialog.CloseRequested += ok => accepted = ok;
            dialog.AcceptCommand.Execute(null);
            Assert.That(accepted, Is.True);

            var allBroken = new LabelOptionsViewModel(Services.LabelSettings, specimens.Take(1).ToList(), "Ensayo", single: false);
            await allBroken.InitializeAsync();
            Assert.That(allBroken.SummaryText, Is.EqualTo("Todas las probetas ya están rotas."));
            Assert.That(allBroken.CanPrint, Is.False);

            var single = new LabelOptionsViewModel(Services.LabelSettings, specimens.Take(1).ToList(), "Probeta", single: true);
            await single.InitializeAsync();
            Assert.That(single.ShowOnlyUntested, Is.False);
            Assert.That(single.LabelCount, Is.EqualTo(1), "one chosen probeta is printed even if broken (a replacement)");
            Assert.That(single.SummaryText, Is.EqualTo("1 etiqueta, en la posición 1."));
        }

        [Test]
        public async Task With_the_filter_on_Rotas_the_labels_are_for_the_broken_probetas()
        {
            await SetServerAsync("servidor-lab");
            var test = await NewTestAsync();
            var pending = await AddAsync(test);
            var broken = await AddAsync(test, load: 40);
            await UseReadableCodesAsync(pending, broken);
            var dialogs = new ScriptedDialogs();
            var page = new TestDetailPageViewModel(Services, dialogs, new RecordingNavigator(), test.Id);
            await page.LoadAsync();

            // No Estado: the usual default, only the unbroken one.
            LabelOptionsViewModel options = null;
            dialogs.Answer(d =>
            {
                options = (LabelOptionsViewModel)d;
                return false;
            });
            page.LabelsCommand.Execute(null);
            Assert.That((options.ShowOnlyUntested, options.OnlyUntested, options.LabelCount), Is.EqualTo((true, true, 1)));

            // Estado = Rotas: the filter's choice wins; the checkbox is hidden and off.
            page.FilterCommand.Execute(null);
            page.FilterPanel.States[3].IsChecked = true;
            await ((AsyncRelayCommand)page.FilterPanel.ApplyCommand).ExecuteAsync();
            ReportPreviewViewModel preview = null;
            dialogs.Answer(d =>
            {
                options = (LabelOptionsViewModel)d;
                return true;
            });
            dialogs.Answer(d =>
            {
                preview = (ReportPreviewViewModel)d;
                return false;
            });
            page.LabelsCommand.Execute(null);
            Assert.That((options.ShowOnlyUntested, options.OnlyUntested, options.LabelCount), Is.EqualTo((false, false, 1)));
            using (var image = preview.Renderer.RenderPage(1, 200))
            {
                Assert.That(LabelLayoutTests.ReadQr(image, LabelSheet.Default, 1), Is.EqualTo("HTTP://SERVIDOR-LAB/P/" + broken.PublicCode));
                Assert.That(LabelLayoutTests.ReadQr(image, LabelSheet.Default, 2), Is.Null);
            }
        }

        [Test]
        public async Task Ensayo_page_prints_labels_for_its_filter_or_for_one_probeta()
        {
            await SetServerAsync("servidor-lab");
            var test = await NewTestAsync();
            var a = await AddAsync(test);
            var b = await AddAsync(test);
            var broken = await AddAsync(test, load: 40);
            await UseReadableCodesAsync(a, b, broken);

            var dialogs = new ScriptedDialogs();
            var page = new TestDetailPageViewModel(Services, dialogs, new RecordingNavigator(), test.Id);
            await page.LoadAsync();
            Assert.That(page.LabelsToolTip, Does.Contain("probetas del ensayo"));

            LabelOptionsViewModel options = null;
            dialogs.Answer(d =>
            {
                options = (LabelOptionsViewModel)d;
                options.StartPosition = 2;
                return true;
            });
            ReportPreviewViewModel preview = null;
            dialogs.Answer(d =>
            {
                preview = (ReportPreviewViewModel)d;
                return false;
            });
            page.LabelsCommand.Execute(null);

            Assert.That(options.ScopeText, Is.EqualTo($"Ensayo Nº {test.Id} · todas sus probetas"));
            Assert.That(options.LabelCount, Is.EqualTo(2));
            Assert.That(preview.Title, Is.EqualTo($"Etiquetas del ensayo Nº {test.Id}"));
            Assert.That(preview.SuggestedFileName, Does.EndWith(" - etiquetas"));
            using (var image = preview.Renderer.RenderPage(1, 200))
            {
                Assert.That(LabelLayoutTests.ReadQr(image, LabelSheet.Default, 2), Is.EqualTo("HTTP://SERVIDOR-LAB/P/" + a.PublicCode));
                Assert.That(LabelLayoutTests.ReadQr(image, LabelSheet.Default, 3), Is.EqualTo("HTTP://SERVIDOR-LAB/P/" + b.PublicCode));
                Assert.That(LabelLayoutTests.ReadQr(image, LabelSheet.Default, 4), Is.Null);
            }

            // Right-click "Imprimir etiqueta…" on a broken probeta: its label alone, e.g. to replace a damaged one.
            page.SelectedRow = page.Rows.Single(r => r.Id == broken.Id);
            Assert.That(page.SpecimenLabelCommand.CanExecute(null), Is.True);
            dialogs.Answer(d =>
            {
                options = (LabelOptionsViewModel)d;
                return true;
            });
            dialogs.Answer(d =>
            {
                preview = (ReportPreviewViewModel)d;
                return false;
            });
            page.SpecimenLabelCommand.Execute(null);
            Assert.That(options.ScopeText, Is.EqualTo($"Probeta Nº {broken.Id} del ensayo Nº {test.Id}"));
            Assert.That(preview.Title, Is.EqualTo($"Etiqueta de la probeta Nº {broken.Id}"));
            using (var image = preview.Renderer.RenderPage(1, 200))
            {
                Assert.That(LabelLayoutTests.ReadQr(image, LabelSheet.Default, 1), Is.EqualTo("HTTP://SERVIDOR-LAB/P/" + broken.PublicCode));
                Assert.That(LabelLayoutTests.ReadQr(image, LabelSheet.Default, 2), Is.Null);
            }

            // Cancelled: nothing opens.
            dialogs.Answer(_ => false);
            page.LabelsCommand.Execute(null);
            Assert.That(dialogs.Shown.Last(), Is.InstanceOf<LabelOptionsViewModel>());
        }

        [Test]
        public async Task Configuracion_etiquetas_matches_presets_validates_and_prints_a_test_sheet()
        {
            await SetServerAsync("servidor-lab");
            var dialogs = new ScriptedDialogs();
            var section = new LabelsSectionViewModel(Services.LabelSettings, Services.Reports, dialogs);
            await section.LoadAsync();

            Assert.That(section.ServerName, Is.EqualTo("servidor-lab"));
            Assert.That(section.SelectedPreset, Is.SameAs(LabelSheet.Presets[0]));
            Assert.That(section.SheetSummary, Is.EqualTo("24 etiquetas por hoja"));
            Assert.That(section.LinkExample, Does.Contain("http://servidor-lab/p/"));

            section.SelectedPreset = LabelSheet.Presets[3];
            Assert.That(section.Columns, Is.EqualTo("3"));
            Assert.That(section.TopMm, Is.EqualTo("15.15"));
            Assert.That(section.SheetSummary, Is.EqualTo("21 etiquetas por hoja"));

            section.LeftMm = "7,5";
            Assert.That(section.SelectedPreset, Is.SameAs(LabelsSectionViewModel.Custom), "changed measurements are a custom sheet");
            Assert.That(section.SheetSummary, Is.EqualTo("21 etiquetas por hoja"), "a comma works as decimal point");
            section.LeftMm = "20";
            Assert.That(section.SheetSummary, Is.Null, "20 + 3 × 63.5 + 2 × 2.5 is wider than A4");

            // Test sheet with the measurements as typed: refused while they don't fit, shown once they do.
            section.TestSheetCommand.Execute(null);
            Assert.That(section.Errors["Sheet"], Does.Contain("no entran"));
            section.LeftMm = "7.25";
            Assert.That(section.SelectedPreset, Is.SameAs(LabelSheet.Presets[3]));
            ReportPreviewViewModel preview = null;
            dialogs.Answer(d =>
            {
                preview = (ReportPreviewViewModel)d;
                return false;
            });
            section.TestSheetCommand.Execute(null);
            Assert.That(preview.Title, Is.EqualTo("Hoja de prueba de etiquetas"));
            Assert.That(preview.PageCount, Is.EqualTo(1));

            section.ServerName = "192.168.1.5";
            section.Rows = "siete";
            await ((AsyncRelayCommand)section.SaveCommand).ExecuteAsync();
            Assert.That(section.Errors["Rows"], Is.EqualTo(NumberInput.InvalidMessage));

            section.Rows = "7";
            await ((AsyncRelayCommand)section.SaveCommand).ExecuteAsync();
            Assert.That(section.Errors["ServerName"], Does.Contain("IP"));

            section.ServerName = "Lab-Server.local";
            await ((AsyncRelayCommand)section.SaveCommand).ExecuteAsync();
            Assert.That(section.MessageKind, Is.EqualTo(MessageKind.Success), section.Message);
            Assert.That(section.ServerName, Is.EqualTo("lab-server.local"));
            var saved = await Services.LabelSettings.GetAsync();
            Assert.That(saved.Sheet.SameAs(LabelSheet.Presets[3].Sheet), Is.True);
        }

        [Test]
        public async Task Configuracion_shows_Etiquetas_to_administrators_only()
        {
            var admin = new SettingsPageViewModel(Services, new ScriptedDialogs(), () => { });
            Assert.That(admin.Find<LabelsSectionViewModel>(), Is.Not.Null);

            await SignInNewOperatorAsync();
            var op = new SettingsPageViewModel(Services, new ScriptedDialogs(), () => { });
            Assert.That(op.Find<LabelsSectionViewModel>(), Is.Null);
        }
    }
}
