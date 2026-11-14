using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services.Reports;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels.CompressionTests;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.Tests.Reports
{
    /// <summary>The ensayo workbook, without a database.</summary>
    public class ExcelExportLayoutTests
    {
        [Test]
        public void Probetas_sheet_has_real_numbers_and_dates_under_a_frozen_heading()
        {
            var data = ReportSamples.Test(null, null, 30);
            using (var workbook = TestExcelExport.Build(data))
            {
                Assert.That(workbook.Worksheets.Select(w => w.Name), Is.EqualTo(new[] { "Probetas", "Ensayo" }));
                var sheet = workbook.Worksheet(TestExcelExport.SpecimensSheet);

                Assert.That(Enumerable.Range(1, 13).Select(c => sheet.Cell(1, c).GetString()), Is.EqualTo(new[]
                {
                    "Nº", "Descripción", "Slump (cm)", "Moldeo", "Rotura", "Edad (días)", "Ø (cm)", "Altura (cm)", "Peso (kg)",
                    "Densidad (kg/m³)", "Carga (t)", "Resistencia (kg/cm²)", "% fck",
                }));
                Assert.That(sheet.Cell(1, 1).Style.Font.Bold, Is.True);
                Assert.That(sheet.SheetView.SplitRow, Is.EqualTo(1), "heading stays when scrolling");
                Assert.That(sheet.AutoFilter.IsEnabled, Is.True);
                Assert.That(sheet.LastRowUsed().RowNumber(), Is.EqualTo(31), "heading + 30 probetas");

                var first = data.Specimens[0];
                Assert.That(sheet.Cell(2, 1).DataType, Is.EqualTo(XLDataType.Number));
                Assert.That(sheet.Cell(2, 1).GetDouble(), Is.EqualTo(5000));
                Assert.That(sheet.Cell(2, 2).GetString(), Is.EqualTo("Losa eje A"));
                Assert.That(sheet.Cell(2, 4).DataType, Is.EqualTo(XLDataType.DateTime), "a date, not text");
                Assert.That(sheet.Cell(2, 4).GetDateTime(), Is.EqualTo(new DateTime(2026, 10, 1)));
                Assert.That(sheet.Cell(2, 4).Style.NumberFormat.Format, Is.EqualTo("dd/mm/yyyy"));
                Assert.That(sheet.Cell(2, 5).GetDateTime(), Is.EqualTo(new DateTime(2026, 10, 8)));
                Assert.That(sheet.Cell(2, 6).GetDouble(), Is.EqualTo(7));
                Assert.That(sheet.Cell(2, 7).GetDouble(), Is.EqualTo(15.02));
                Assert.That(sheet.Cell(2, 9).GetDouble(), Is.EqualTo((double)first.WeightKg).Within(1e-9), "full precision, shown with 3 decimals");
                Assert.That(sheet.Cell(2, 9).Style.NumberFormat.Format, Is.EqualTo("0.000"));
                Assert.That(sheet.Cell(2, 12).GetDouble(), Is.EqualTo(first.StrengthKgPerCm2.Value).Within(1e-9));
                Assert.That(sheet.Cell(2, 13).GetDouble(), Is.EqualTo(first.StrengthKgPerCm2.Value / 210 * 100).Within(1e-9));

                // The last 4 probetas are not broken yet: empty cells, not zeros.
                Assert.That(sheet.Cell(31, 10).IsEmpty(), Is.False, "density doesn't need the rupture");
                Assert.That(sheet.Cell(31, 11).IsEmpty(), Is.True, "Carga");
                Assert.That(sheet.Cell(31, 12).IsEmpty(), Is.True, "Resistencia");
                Assert.That(sheet.Cell(31, 13).IsEmpty(), Is.True, "% fck");
            }
        }

        [Test]
        public void Ensayo_sheet_has_the_test_data_and_the_mix()
        {
            var data = ReportSamples.Test(null, null, 3);
            using (var workbook = TestExcelExport.Build(data))
            {
                var sheet = workbook.Worksheet(TestExcelExport.TestSheet);
                string Value(string label) => sheet.Column(1).CellsUsed().Single(c => c.GetString() == label).CellRight().GetFormattedString();

                Assert.That(Value("Ensayo Nº"), Is.EqualTo("1234"));
                Assert.That(Value("Fecha"), Is.EqualTo("01/10/2026"));
                Assert.That(Value("Cliente"), Is.EqualTo("Constructora Andina S.R.L."));
                Assert.That(Value("Proyecto / obra"), Is.EqualTo("Edificio Torre Norte — losa 3er piso"));
                Assert.That(Value("Dosificación"), Is.EqualTo("H-21 bombeable"));
                Assert.That(Value("fck (kg/cm²)"), Is.EqualTo("210"));
                Assert.That(Value("Slump de diseño (cm)"), Is.EqualTo("10"));
                Assert.That(Value("Relación agua/cemento"), Is.EqualTo("0.53"));
                Assert.That(Value("Exportado"), Is.EqualTo("12/11/2026"));

                var cement = sheet.Column(1).CellsUsed().Single(c => c.GetString() == "Cemento IP-30");
                Assert.That(cement.CellRight().GetString(), Is.EqualTo("Itacamba"));
                Assert.That(cement.CellRight(2).GetDouble(), Is.EqualTo(350));
                var total = sheet.Column(1).CellsUsed().Single(c => c.GetString() == "Total");
                Assert.That(total.CellRight(2).GetDouble(), Is.EqualTo(2367.8));
            }
        }

        [Test]
        public void Ensayo_without_probetas_or_design_still_exports()
        {
            var data = ReportSamples.Test(null, null, 0);
            data.Design = null;
            var path = Path.Combine(Path.GetTempPath(), "ProbetaControl-test-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                TestExcelExport.Save(data, path);
                using (var workbook = new XLWorkbook(path))
                {
                    Assert.That(workbook.Worksheet("Probetas").LastRowUsed().RowNumber(), Is.EqualTo(1), "only the heading");
                    Assert.That(workbook.Worksheet("Ensayo").Column(1).CellsUsed().Any(c => c.GetString() == "Material"), Is.False);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>"Excel" on the ensayo page, against a database.</summary>
    public class ExcelExportTests : ServiceFixture
    {
        public ExcelExportTests(TestServer server)
            : base(server)
        {
        }

        private static string TempPath() => Path.Combine(Path.GetTempPath(), "ProbetaControl-test-" + Guid.NewGuid().ToString("N") + ".xlsx");

        [Test]
        public async Task Excel_exports_the_filtered_probetas_without_archived_ones()
        {
            var test = await NewTestAsync();
            var ids = new int[5];
            for (var i = 0; i < 5; i++)
            {
                var specimen = NewSpecimen(test.Id, Today.AddDays(-30), 28, load: i == 4 ? (decimal?)null : 40);
                await Services.Specimens.SaveAsync(specimen, testUnlocked: true);
                ids[i] = specimen.Id;
            }

            await Services.Specimens.SetArchivedAsync(await Services.Specimens.GetAsync(ids[1]), true);

            var dialogs = new ScriptedDialogs();
            var page = new TestDetailPageViewModel(Services, dialogs, new RecordingNavigator(), test.Id);
            await page.OnNavigatedToAsync();
            Assert.That(page.ExportExcelToolTip, Does.Contain("todas las probetas"));

            var path = TempPath();
            try
            {
                // Whole ensayo: 4 active probetas, the last one not broken.
                MessageDialogViewModel saved = null;
                dialogs.AnswerFile(path);
                dialogs.Answer(d =>
                {
                    saved = (MessageDialogViewModel)d;
                    return true;
                });
                await ((AsyncRelayCommand)page.ExportExcelCommand).ExecuteAsync();

                Assert.That(dialogs.SuggestedFileNames.Single(), Does.StartWith("2026-02-08 Ensayo " + test.Id + " - "));
                Assert.That(saved.Text, Is.EqualTo("Archivo guardado:\n" + path));
                Assert.That(page.IsBusy, Is.False);
                using (var workbook = new XLWorkbook(path))
                {
                    var sheet = workbook.Worksheet("Probetas");
                    Assert.That(sheet.LastRowUsed().RowNumber() - 1, Is.EqualTo(4));
                    Assert.That(sheet.Column(1).CellsUsed().Skip(1).Select(c => (int)c.GetDouble()), Is.EqualTo(new[] { ids[0], ids[2], ids[3], ids[4] }));
                    Assert.That(sheet.Cell(2, 12).GetDouble(), Is.EqualTo(226.35).Within(0.01), "40 t on Ø 15 cm");
                    Assert.That(sheet.Cell(5, 12).IsEmpty(), Is.True, "not broken");
                    Assert.That(workbook.Worksheet("Ensayo").Cell(1, 2).GetDouble(), Is.EqualTo(test.Id));
                }

                // With a filter, only its probetas; the existing file is replaced.
                dialogs.Answer(d =>
                {
                    var filter = (SpecimenFilterViewModel)d;
                    filter.IdRanges = $"{ids[0]}-{ids[2]}";
                    filter.ApplyCommand.Execute(null);
                    return true;
                });
                await ((AsyncRelayCommand)page.FilterCommand).ExecuteAsync();
                Assert.That(page.ExportExcelToolTip, Does.Contain("filtro"));
                dialogs.AnswerFile(path);
                dialogs.Answer(_ => true);
                await ((AsyncRelayCommand)page.ExportExcelCommand).ExecuteAsync();
                using (var workbook = new XLWorkbook(path))
                {
                    Assert.That(workbook.Worksheet("Probetas").LastRowUsed().RowNumber() - 1, Is.EqualTo(2), "ids[0] and ids[2]; ids[1] is archived");
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public async Task Cancelling_the_save_dialog_exports_nothing_and_a_locked_file_is_explained()
        {
            var test = await NewTestAsync();
            var dialogs = new ScriptedDialogs();
            var page = new TestDetailPageViewModel(Services, dialogs, new RecordingNavigator(), test.Id);
            await page.OnNavigatedToAsync();

            dialogs.AnswerFile(null);
            await ((AsyncRelayCommand)page.ExportExcelCommand).ExecuteAsync();
            Assert.That(dialogs.Shown, Is.Empty, "no message after cancelling");

            // The file is open in another program (Excel keeps it locked).
            var path = TempPath();
            try
            {
                using (new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    MessageDialogViewModel warning = null;
                    dialogs.AnswerFile(path);
                    dialogs.Answer(d =>
                    {
                        warning = (MessageDialogViewModel)d;
                        return true;
                    });
                    await ((AsyncRelayCommand)page.ExportExcelCommand).ExecuteAsync();

                    Assert.That(warning.Title, Is.EqualTo("No se pudo guardar el archivo"));
                    Assert.That(warning.Text, Does.Contain("Si está abierto en Excel, ciérrelo y vuelva a exportar."));
                    Assert.That(page.IsBusy, Is.False);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
