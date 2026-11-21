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
        private static readonly string Assets = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "../../../../../src/ProbetaControl/Assets"));

        private static TestReportData Sample(int specimens) => ReportSamples.Test(
            File.ReadAllBytes(Path.Combine(Assets, "default-logo.png")),
            File.ReadAllBytes(Path.Combine(Assets, "default-stamp.png")),
            specimens);

        /// <summary>The value next to a label of the test data block (label merged A:B, value from C).</summary>
        private static IXLCell FieldValue(IXLWorksheet sheet, string label) =>
            sheet.Column(1).CellsUsed().Single(c => c.GetString() == label).CellRight(2);

        [Test]
        public void One_sheet_with_the_report_header_logo_data_and_mix()
        {
            var data = Sample(30);
            using (var workbook = TestExcelExport.Build(data))
            {
                Assert.That(workbook.Worksheets.Select(w => w.Name), Is.EqualTo(new[] { "Ensayo" }));
                var sheet = workbook.Worksheet(TestExcelExport.SheetName);

                Assert.That(sheet.Cell(1, 3).GetString(), Is.EqualTo(TestReport.Title));
                Assert.That(sheet.Cell(2, 3).GetString(), Is.EqualTo("Servicios de Ingeniería HJVA Ltda."));
                Assert.That(sheet.Cell(1, 11).GetString(), Is.EqualTo("Ensayo Nº 1234"));
                Assert.That(sheet.Cell(2, 11).GetString(), Is.EqualTo("Fecha: 01/10/2026"));
                Assert.That(sheet.Cell(3, 11).GetString(), Is.EqualTo("Exportado: 12/11/2026"));

                // Logo at the top left, chart beside the summary, stamp at the end.
                Assert.That(sheet.Pictures.Count, Is.EqualTo(3));
                var logo = sheet.Pictures.OrderBy(p => p.TopLeftCell.Address.RowNumber).First();
                Assert.That(logo.TopLeftCell.Address.ToString(), Is.EqualTo("A1"));
                Assert.That(logo.Height, Is.LessThanOrEqualTo(65), "fits the three header rows (17 mm)");

                Assert.That(FieldValue(sheet, "Cliente").GetString(), Is.EqualTo("Constructora Andina S.R.L."));
                Assert.That(FieldValue(sheet, "Proyecto / obra").GetString(), Is.EqualTo("Edificio Torre Norte — losa 3er piso"));
                Assert.That(FieldValue(sheet, "Dosificación").GetString(), Is.EqualTo("H-21 bombeable"));
                var fck = FieldValue(sheet, "Resistencia de diseño (fck)");
                Assert.That(fck.GetDouble(), Is.EqualTo(210), "a number");
                Assert.That(fck.Style.NumberFormat.Format, Is.EqualTo("#,##0 \"kg/cm²\""));
                Assert.That(FieldValue(sheet, "Relación agua/cemento").GetDouble(), Is.EqualTo(0.53));

                var cement = sheet.Column(9).CellsUsed().Single(c => c.GetString() == "Cemento IP-30");
                Assert.That(cement.CellRight(2).GetString(), Is.EqualTo("Itacamba"));
                Assert.That(sheet.Cell(cement.Address.RowNumber, 13).GetDouble(), Is.EqualTo(350));
                var total = sheet.Column(9).CellsUsed().Single(c => c.GetString() == "Total");
                var totalCell = sheet.Cell(total.Address.RowNumber, 13);
                Assert.That(totalCell.GetDouble(), Is.EqualTo(2367.8));
                Assert.That(totalCell.Style.NumberFormat.Format, Is.EqualTo("#,##0.0"), "grouped, only the decimals it has");

                Assert.That(sheet.Column(1).CellsUsed().Any(c => c.GetString().StartsWith("NOTA:")), Is.True);
                Assert.That(sheet.PageSetup.PageOrientation, Is.EqualTo(XLPageOrientation.Landscape));
                Assert.That(sheet.PageSetup.PagesWide, Is.EqualTo(1));
            }
        }

        [Test]
        public void Probetas_are_a_table_with_real_numbers_and_dates()
        {
            var data = Sample(30);
            using (var workbook = TestExcelExport.Build(data))
            {
                var sheet = workbook.Worksheet(TestExcelExport.SheetName);
                var table = sheet.Table(TestExcelExport.TableName);
                Assert.That(table.Fields.Select(f => f.Name), Is.EqualTo(new[]
                {
                    "Nº", "Descripción", "Slump (cm)", "Moldeo", "Rotura", "Edad (días)", "Ø (cm)", "Altura (cm)", "Peso (kg)",
                    "Densidad (kg/m³)", "Carga (t)", "Resistencia (kg/cm²)", "% fck",
                }));
                Assert.That(table.ShowAutoFilter, Is.True);
                Assert.That(table.DataRange.RowCount(), Is.EqualTo(30));
                var top = table.HeadersRow().RowNumber();
                Assert.That(sheet.PageSetup.FirstRowToRepeatAtTop, Is.EqualTo(top), "heading on every printed page");

                var first = data.Specimens[0];
                var row = top + 1;
                Assert.That(sheet.Cell(row, 1).GetDouble(), Is.EqualTo(5000));
                Assert.That(sheet.Cell(row, 1).Style.NumberFormat.Format, Is.EqualTo("0"), "IDs are never grouped");
                Assert.That(sheet.Cell(row, 2).GetString(), Is.EqualTo("Losa eje A"));
                Assert.That(sheet.Cell(row, 4).DataType, Is.EqualTo(XLDataType.DateTime), "a date, not text");
                Assert.That(sheet.Cell(row, 4).GetDateTime(), Is.EqualTo(new DateTime(2026, 10, 1)));
                Assert.That(sheet.Cell(row, 4).Style.NumberFormat.Format, Is.EqualTo("dd/mm/yyyy"));
                Assert.That(sheet.Cell(row, 6).GetDouble(), Is.EqualTo(7));
                Assert.That(sheet.Cell(row, 7).GetDouble(), Is.EqualTo(15.02));
                Assert.That(sheet.Cell(row, 9).GetDouble(), Is.EqualTo((double)first.WeightKg).Within(1e-9), "full precision, shown with 3 decimals");
                Assert.That(sheet.Cell(row, 9).Style.NumberFormat.Format, Is.EqualTo("#,##0.000"));
                Assert.That(sheet.Cell(row, 10).Style.NumberFormat.Format, Is.EqualTo("#,##0"), "density in the thousands is grouped");
                Assert.That(sheet.Cell(row, 12).GetDouble(), Is.EqualTo(first.StrengthKgPerCm2.Value).Within(1e-9));
                Assert.That(sheet.Cell(row, 13).GetDouble(), Is.EqualTo(first.StrengthKgPerCm2.Value / 210 * 100).Within(1e-9));

                // The last 4 probetas are not broken yet: empty cells, not zeros.
                var last = top + 30;
                Assert.That(sheet.Cell(last, 10).IsEmpty(), Is.False, "density doesn't need the rupture");
                Assert.That(sheet.Cell(last, 11).IsEmpty(), Is.True, "Carga");
                Assert.That(sheet.Cell(last, 12).IsEmpty(), Is.True, "Resistencia");
                Assert.That(sheet.Cell(last, 13).IsEmpty(), Is.True, "% fck");

                // Summary by age under the table.
                var summary = sheet.Column(1).CellsUsed().Single(c => c.GetString() == "Resumen por edad");
                Assert.That(summary.Address.RowNumber, Is.GreaterThan(last));
                Assert.That(sheet.Cell(summary.Address.RowNumber + 2, 1).GetDouble(), Is.EqualTo(7), "first age");
            }
        }

        [Test]
        public void Ensayo_without_probetas_design_or_images_still_exports()
        {
            var data = ReportSamples.Test(null, null, 0);
            data.Design = null;
            var path = Path.Combine(Path.GetTempPath(), "ProbetaControl-test-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                TestExcelExport.Save(data, path);
                using (var workbook = new XLWorkbook(path))
                {
                    var sheet = workbook.Worksheet("Ensayo");
                    Assert.That(sheet.Tables.Count(), Is.EqualTo(0), "no probetas: just the heading");
                    Assert.That(sheet.Pictures.Count, Is.EqualTo(0));
                    Assert.That(sheet.CellsUsed().Any(c => c.GetString() == "Sin materiales registrados."), Is.True);
                    Assert.That(sheet.CellsUsed().Any(c => c.GetString().StartsWith("Todavía no hay probetas rotas")), Is.True);
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
                Assert.That(saved.IsWarning, Is.False, "good news: no warning sign");
                Assert.That(page.IsBusy, Is.False);
                using (var workbook = new XLWorkbook(path))
                {
                    var sheet = workbook.Worksheet("Ensayo");
                    var rows = sheet.Table("Probetas").DataRange;
                    Assert.That(rows.RowCount(), Is.EqualTo(4));
                    Assert.That(rows.Column(1).Cells().Select(c => (int)c.GetDouble()), Is.EqualTo(new[] { ids[0], ids[2], ids[3], ids[4] }));
                    Assert.That(rows.Cell(1, 12).GetDouble(), Is.EqualTo(226.35).Within(0.01), "40 t on Ø 15 cm");
                    Assert.That(rows.Cell(4, 12).IsEmpty(), Is.True, "not broken");
                    Assert.That(sheet.Cell(1, 11).GetString(), Is.EqualTo("Ensayo Nº " + test.Id));
                }

                // With a filter, only its probetas; the existing file is replaced.
                page.FilterCommand.Execute(null);
                page.FilterPanel.NumbersOrText = $"{ids[0]}-{ids[2]}";
                await ((AsyncRelayCommand)page.FilterPanel.ApplyCommand).ExecuteAsync();
                Assert.That(page.ExportExcelToolTip, Does.Contain("filtro"));
                dialogs.AnswerFile(path);
                dialogs.Answer(_ => true);
                await ((AsyncRelayCommand)page.ExportExcelCommand).ExecuteAsync();
                using (var workbook = new XLWorkbook(path))
                {
                    Assert.That(workbook.Worksheet("Ensayo").Table("Probetas").DataRange.RowCount(), Is.EqualTo(2), "ids[0] and ids[2]; ids[1] is archived");
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
                    Assert.That(warning.IsWarning, Is.True);
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
