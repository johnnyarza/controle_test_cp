using System;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Services.Reports;
using ProbetaControl.ViewModels.Reports;

namespace ProbetaControl.Tests.Reports
{
    /// <summary>The ensayo report layout and the three outputs (screen, printer, PDF), without a database.</summary>
    public class ReportRenderingTests
    {
        private static readonly string Assets = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "../../../../../src/ProbetaControl/Assets"));

        private static TestReportData Sample(int specimens = 30) => ReportSamples.Test(
            File.ReadAllBytes(Path.Combine(Assets, "default-logo.png")),
            File.ReadAllBytes(Path.Combine(Assets, "default-stamp.png")),
            specimens);

        /// <summary>The probetas table: the only one with 13 columns.</summary>
        internal static Table ResultsTable(Document document) =>
            document.Sections[0].Elements.OfType<Table>().Single(t => t.Columns.Count == 13);

        [Test]
        public void Ensayo_report_is_landscape_with_one_row_per_probeta_and_a_repeated_heading()
        {
            var document = TestReport.Build(Sample(30));
            var renderer = new ReportRenderer(document);

            Assert.That(renderer.PageCount, Is.EqualTo(2));
            var size = renderer.PageSize(1);
            Assert.That(size.Width, Is.EqualTo(297 / 25.4 * 72).Within(0.5), "A4 landscape");
            Assert.That(size.Height, Is.EqualTo(210 / 25.4 * 72).Within(0.5));

            var table = ResultsTable(document);
            Assert.That(table.Rows.Count, Is.EqualTo(31), "heading + 30 probetas");
            Assert.That(table.Rows[0].HeadingFormat, Is.True, "repeated on every page");
        }

        [Test]
        public void Letter_paper_setting_changes_the_page_size()
        {
            var data = Sample(3);
            data.Branding.Settings.PaperSize = ReportPaperSize.Letter;
            var size = new ReportRenderer(TestReport.Build(data)).PageSize(1);

            Assert.That(size.Width, Is.EqualTo(792).Within(0.5), "11 in");
            Assert.That(size.Height, Is.EqualTo(612).Within(0.5), "8.5 in");
        }

        [Test]
        public void Report_without_probetas_images_or_texts_still_renders()
        {
            var data = Sample(0);
            var bare = new TestReportData
            {
                Test = data.Test,
                Design = null,
                Specimens = data.Specimens,
                Branding = new ReportBranding(new ReportSettings(), null, null),
                PrintedOn = data.PrintedOn,
            };

            var renderer = new ReportRenderer(TestReport.Build(bare));

            Assert.That(renderer.PageCount, Is.EqualTo(1));
            using (var page = renderer.RenderPage(1, 72))
            {
                Assert.That(page.Width, Is.EqualTo(842).Within(1));
            }
        }

        [Test]
        public void Unbroken_probetas_show_a_dash_instead_of_load_and_strength()
        {
            var table = ResultsTable(TestReport.Build(Sample(30)));
            var last = table.Rows[table.Rows.Count - 1];

            Assert.That(CellText(last.Cells[10]), Is.EqualTo("—"), "Carga");
            Assert.That(CellText(last.Cells[11]), Is.EqualTo("—"), "Resistencia");
            Assert.That(CellText(last.Cells[12]), Is.Empty, "% fck");
            Assert.That(CellText(table.Rows[1].Cells[11]), Does.Match(@"^\d+\.\d\d$"), "a point as decimal separator");
        }

        [Test]
        public void Saved_pdf_is_a_real_pdf_with_every_page()
        {
            var path = Path.Combine(Path.GetTempPath(), "ProbetaControl-test-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                new ReportRenderer(TestReport.Build(Sample(30))).SavePdf(path);

                var bytes = File.ReadAllBytes(path);
                Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 0, 5), Is.EqualTo("%PDF-"));
                var text = System.Text.Encoding.ASCII.GetString(bytes);
                Assert.That(System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page[^s]").Count, Is.EqualTo(2));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Printing_chooses_the_reports_paper_and_prints_each_page()
        {
            const string Printer = "Microsoft Print to PDF";
            Assume.That(PrinterSettings.InstalledPrinters.Cast<string>(), Does.Contain(Printer), "needs the Windows PDF printer");

            var preview = new PreviewPrintController();
            new ReportRenderer(TestReport.Build(Sample(30))).Print(new PrinterSettings { PrinterName = Printer }, "prueba", preview);

            var pages = preview.GetPreviewPageInfo();
            Assert.That(pages.Length, Is.EqualTo(2));
            Assert.That(pages[0].PhysicalSize.Width, Is.EqualTo(1169).Within(2), "A4 landscape in 1/100 inch, not the printer's default Letter");
            Assert.That(pages[0].PhysicalSize.Height, Is.EqualTo(827).Within(2));
        }

        [Test]
        public void Printing_a_page_range_prints_only_those_pages()
        {
            const string Printer = "Microsoft Print to PDF";
            Assume.That(PrinterSettings.InstalledPrinters.Cast<string>(), Does.Contain(Printer), "needs the Windows PDF printer");

            var preview = new PreviewPrintController();
            var settings = new PrinterSettings { PrinterName = Printer, PrintRange = PrintRange.SomePages, FromPage = 2, ToPage = 2 };
            new ReportRenderer(TestReport.Build(Sample(30))).Print(settings, "prueba", preview);

            Assert.That(preview.GetPreviewPageInfo().Length, Is.EqualTo(1));
        }

        [Test]
        public void Chart_is_left_out_until_there_are_results()
        {
            Assert.That(StrengthChart.RenderPng(Array.Empty<(int, double)>(), 210, 100, 60), Is.Null);
            var png = StrengthChart.RenderPng(new[] { (7, 150.0), (28, 230.0) }, 210, 100, 60);
            Assert.That(png.Take(4), Is.EqualTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
        }

        [TestCase(0, null)]
        [TestCase(0.5, null)]
        [TestCase(1, 1)]
        [TestCase(9, 1)]
        [TestCase(9.5, 2)]
        [TestCase(15, 2)]
        [TestCase(15.5, 3)]
        public void Slump_tolerance_follows_the_legacy_report(decimal slump, int? tolerance)
        {
            Assert.That(Calculators.SlumpToleranceCm(slump), Is.EqualTo(tolerance));
        }

        [Test]
        public void Slump_text_on_the_report()
        {
            Assert.That(TestReport.SlumpText(10), Is.EqualTo("10 ± 2 cm"));
            Assert.That(TestReport.SlumpText(7.5m), Is.EqualTo("7.5 ± 1 cm"));
            Assert.That(TestReport.SlumpText(null), Is.EqualTo("—"));
        }

        [Test]
        public void Suggested_file_names_are_valid_on_windows()
        {
            Assert.That(ReportPreviewViewModel.SafeFileName("2026-10-01 Ensayo 3 - A/B: \"obra\""), Is.EqualTo("2026-10-01 Ensayo 3 - A-B- -obra-"));
            var test = new CompressionTest { Id = 12, TestDate = new DateTime(2026, 10, 1), ClientName = "Constructora", ProjectName = "Torre" };
            Assert.That(ReportService.TestFileName(test), Is.EqualTo("2026-10-01 Ensayo 12 - Constructora - Torre"));
        }

        internal static string CellText(Cell cell) =>
            string.Concat(cell.Elements.OfType<Paragraph>().SelectMany(p => p.Elements.OfType<MigraDoc.DocumentObjectModel.Text>()).Select(t => t.Content));
    }
}
