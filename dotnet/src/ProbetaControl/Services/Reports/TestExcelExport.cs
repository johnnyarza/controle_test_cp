using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using ProbetaControl.Models;

namespace ProbetaControl.Services.Reports
{
    /// <summary>
    /// The ensayo as an Excel workbook, laid out like the ensayo report (<see cref="TestReport"/>): header with the
    /// logo, title, lab name, ensayo Nº and dates; the test data beside the mix; the probetas as an Excel table (real
    /// numbers and dates, filter arrows); the summary by age with the chart as a picture; note and stamp. One sheet,
    /// set up to print like the report (landscape, the report's paper, fitted to the width, footer).
    /// Number formats are Excel's, so Excel shows the PC's decimal and thousands separators.
    /// </summary>
    public static class TestExcelExport
    {
        public const string FileFilter = "Libro de Excel (*.xlsx)|*.xlsx";
        public const string SheetName = "Ensayo";
        public const string TableName = "Probetas";

        /// <summary>Excel's own date format: shows as dd/mm/yyyy whatever the PC's regional settings.</summary>
        private const string DateFormat = "dd/mm/yyyy";

        private const string IdFormat = "0";

        private static readonly XLColor HeadingFill = XLColor.FromArgb(0xE8, 0xEC, 0xF1);
        private static readonly XLColor Rule = XLColor.FromArgb(0xBB, 0xBB, 0xBB);

        /// <summary>Probetas table columns: heading, width (characters), number format.</summary>
        private static readonly (string Heading, double Width, string Format)[] Columns =
        {
            ("Nº", 9, IdFormat),
            ("Descripción", 28, null),
            ("Slump (cm)", 11, "#,##0.0"),
            ("Moldeo", 12, DateFormat),
            ("Rotura", 12, DateFormat),
            ("Edad (días)", 11, "#,##0"),
            ("Ø (cm)", 9, "#,##0.00"),
            ("Altura (cm)", 11, "#,##0.00"),
            ("Peso (kg)", 11, "#,##0.000"),
            ("Densidad (kg/m³)", 16, "#,##0"),
            ("Carga (t)", 10, "#,##0.00"),
            ("Resistencia (kg/cm²)", 19, "#,##0.00"),
            ("% fck", 10, "#,##0"),
        };

        private static int LastColumn => Columns.Length;

        /// <summary>Writes the workbook to <paramref name="path"/>, replacing an existing file.</summary>
        public static void Save(TestReportData data, string path)
        {
            using (var workbook = Build(data))
            {
                workbook.SaveAs(path);
            }
        }

        public static XLWorkbook Build(TestReportData data)
        {
            var workbook = new XLWorkbook();
            workbook.Properties.Title = string.Format(CultureInfo.InvariantCulture, "Ensayo Nº {0}", data.Test.Id);
            workbook.Properties.Author = data.Branding?.Settings.LabName ?? string.Empty;
            var sheet = workbook.AddWorksheet(SheetName);
            sheet.ShowGridLines = false;
            sheet.Style.Font.FontName = "Calibri";
            sheet.Style.Font.FontSize = 10;
            for (var i = 0; i < Columns.Length; i++)
            {
                sheet.Column(i + 1).Width = Columns[i].Width;
            }

            var row = AddHeader(sheet, data);
            row = AddTestData(sheet, data, row + 1);
            var headingRow = AddSpecimens(sheet, data, row + 1, out row);
            row = AddSummary(sheet, data, row + 1);
            AddNoteAndStamp(sheet, data.Branding, row + 1);
            SetUpPrinting(sheet, data.Branding, headingRow);
            return workbook;
        }

        /// <summary>Rows 1–3: logo, title and lab name, ensayo Nº and dates; a rule under them. Returns the last row.</summary>
        private static int AddHeader(IXLWorksheet sheet, TestReportData data)
        {
            sheet.Row(1).Height = 22;
            sheet.Row(2).Height = 16;
            sheet.Row(3).Height = 16;
            AddPicture(sheet, data.Branding?.Logo, sheet.Cell(1, 1), 46, 17);

            var title = sheet.Range(1, 3, 1, 10).Merge();
            title.Value = TestReport.Title;
            title.Style.Font.Bold = true;
            title.Style.Font.FontSize = 12;
            title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            title.Style.Alignment.ShrinkToFit = true;

            var labName = data.Branding?.Settings.LabName;
            if (!string.IsNullOrWhiteSpace(labName))
            {
                var lab = sheet.Range(2, 3, 2, 10).Merge();
                lab.Value = labName;
                lab.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            var number = sheet.Range(1, 11, 1, LastColumn).Merge();
            number.Value = string.Format(CultureInfo.InvariantCulture, "Ensayo Nº {0}", data.Test.Id);
            number.Style.Font.Bold = true;
            number.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            number.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var date = sheet.Range(2, 11, 2, LastColumn).Merge();
            date.Value = "Fecha: " + AppCulture.FormatDate(data.Test.TestDate);
            date.Style.Font.Bold = true;
            date.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var exported = sheet.Range(3, 11, 3, LastColumn).Merge();
            exported.Value = "Exportado: " + AppCulture.FormatDate(data.PrintedOn);
            exported.Style.Font.FontSize = 8.5;
            exported.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            sheet.Range(3, 1, 3, LastColumn).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            return 3;
        }

        /// <summary>The test's data on the left (A:G), the mix on the right (I:M). Returns the last row used.</summary>
        private static int AddTestData(IXLWorksheet sheet, TestReportData data, int top)
        {
            var test = data.Test;
            var design = data.Design;
            var row = top;
            void Field(string label, XLCellValue value, string format = null)
            {
                var name = sheet.Range(row, 1, row, 2).Merge();
                name.Value = label;
                name.Style.Font.Bold = true;
                var cell = sheet.Range(row, 3, row, 7).Merge();
                cell.Value = value;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                name.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                if (format != null)
                {
                    cell.Style.NumberFormat.Format = format;
                }

                // Merged cells don't grow by themselves: give long text the lines it needs (about 55 characters each).
                if (value.IsText && value.GetText().Length > 55)
                {
                    cell.Style.Alignment.WrapText = true;
                    sheet.Row(row).Height = 13.5 * Math.Min(4, (value.GetText().Length + 54) / 55);
                }

                row++;
            }

            Field("Cliente", test.ClientName ?? string.Empty);
            Field("Proveedor del hormigón", test.ConcreteProviderName ?? string.Empty);
            Field("Proyecto / obra", test.ProjectName ?? string.Empty);
            Field("Ubicación", test.Address ?? string.Empty);
            Field("Dosificación", test.ConcreteDesignDescription ?? string.Empty);
            Field("Resistencia de diseño (fck)", test.ConcreteDesignFck, Flexible(test.ConcreteDesignFck, " \"kg/cm²\""));
            Field("Slump de diseño", TestReport.SlumpText(design?.Slump));
            var ratio = design?.WaterCementRatio;
            if (ratio.HasValue)
            {
                Field("Relación agua/cemento", Math.Round(ratio.Value, 2), "0.00");
            }

            return Math.Max(row - 1, AddMix(sheet, design, top));
        }

        /// <summary>"Dosificación por m³ de hormigón": material (I:J), provider (K:L), kg/m³ (M). Returns the last row.</summary>
        private static int AddMix(IXLWorksheet sheet, ConcreteDesign design, int top)
        {
            const int First = 9;
            var caption = sheet.Range(top, First, top, LastColumn).Merge();
            caption.Value = "Dosificación por m³ de hormigón";
            caption.Style.Font.Bold = true;
            var materials = design?.UsedMaterials ?? Array.Empty<ConcreteDesignMaterial>();
            if (materials.Count == 0)
            {
                var none = sheet.Range(top + 1, First, top + 1, LastColumn).Merge();
                none.Value = "Sin materiales registrados.";
                none.Style.Font.FontSize = 8.5;
                return top + 1;
            }

            var row = top + 1;
            sheet.Range(row, First, row, First + 1).Merge().Value = "Material";
            sheet.Range(row, First + 2, row, First + 3).Merge().Value = "Proveedor";
            sheet.Cell(row, LastColumn).Value = "kg/m³";
            var heading = sheet.Range(row, First, row, LastColumn);
            heading.Style.Font.Bold = true;
            heading.Style.Fill.BackgroundColor = HeadingFill;
            foreach (var material in materials)
            {
                row++;
                sheet.Range(row, First, row, First + 1).Merge().Value = material.MaterialName ?? string.Empty;
                sheet.Range(row, First + 2, row, First + 3).Merge().Value = material.ProviderName ?? string.Empty;
                if (material.QuantityKgPerM3.HasValue)
                {
                    var quantity = sheet.Cell(row, LastColumn);
                    quantity.Value = material.QuantityKgPerM3.Value;
                    quantity.Style.NumberFormat.Format = Flexible(material.QuantityKgPerM3.Value);
                }
            }

            row++;
            sheet.Range(row, First, row, First + 3).Merge().Value = "Total";
            var total = sheet.Cell(row, LastColumn);
            total.Value = design.TotalKgPerM3;
            total.Style.NumberFormat.Format = Flexible(design.TotalKgPerM3);
            sheet.Range(row, First, row, LastColumn).Style.Font.Bold = true;
            Grid(sheet.Range(top + 1, First, row, LastColumn));
            sheet.Cell(row, First).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            return row;
        }

        /// <summary>"Resultados" and the probetas table. Returns the table's heading row; <paramref name="last"/> = its last row.</summary>
        private static int AddSpecimens(IXLWorksheet sheet, TestReportData data, int top, out int last)
        {
            Heading(sheet, top, "Resultados");
            var headingRow = top + 1;
            for (var i = 0; i < Columns.Length; i++)
            {
                sheet.Cell(headingRow, i + 1).Value = Columns[i].Heading;
            }

            var fck = data.Test.ConcreteDesignFck;
            var row = headingRow;
            foreach (var s in data.Specimens)
            {
                row++;
                sheet.Cell(row, 1).Value = s.Id;
                sheet.Cell(row, 2).Value = s.Code ?? string.Empty;
                Set(sheet.Cell(row, 3), s.Slump);
                sheet.Cell(row, 4).Value = s.MoldDate.Date;
                sheet.Cell(row, 5).Value = s.RuptureDate.Date;
                sheet.Cell(row, 6).Value = s.AgeDays;
                sheet.Cell(row, 7).Value = s.DiameterCm;
                sheet.Cell(row, 8).Value = s.HeightCm;
                Set(sheet.Cell(row, 9), s.WeightKg);
                Set(sheet.Cell(row, 10), s.DensityKgPerM3);

                // Not broken yet: the cells stay empty, so averages in Excel skip them (the legacy file had 0).
                if (s.IsTested)
                {
                    Set(sheet.Cell(row, 11), s.RuptureLoadTon);
                    Set(sheet.Cell(row, 12), s.StrengthKgPerCm2);
                    Set(sheet.Cell(row, 13), SpecimenMath.PercentOfFck(s.StrengthKgPerCm2, fck));
                }
            }

            if (row > headingRow)
            {
                for (var i = 0; i < Columns.Length; i++)
                {
                    if (Columns[i].Format != null)
                    {
                        sheet.Range(headingRow + 1, i + 1, row, i + 1).Style.NumberFormat.Format = Columns[i].Format;
                    }
                }

                // An Excel table: filter arrows, and its headings replace the column letters when scrolling down.
                var table = sheet.Range(headingRow, 1, row, LastColumn).CreateTable(TableName);
                table.Theme = XLTableTheme.None;
                table.ShowRowStripes = false;
                table.ShowAutoFilter = true;
            }

            var heading = sheet.Range(headingRow, 1, headingRow, LastColumn);
            heading.Style.Font.Bold = true;
            heading.Style.Fill.BackgroundColor = HeadingFill;
            heading.Style.Alignment.WrapText = true;
            heading.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
            sheet.Row(headingRow).Height = 27;
            Grid(sheet.Range(headingRow, 1, row, LastColumn));
            last = row;
            return headingRow;
        }

        /// <summary>"Resumen por edad" (A:E) beside the strength chart (G:M), like the report. Returns the last row used.</summary>
        private static int AddSummary(IXLWorksheet sheet, TestReportData data, int top)
        {
            var tested = data.Specimens.Where(s => s.StrengthKgPerCm2.HasValue).ToList();
            if (tested.Count == 0)
            {
                var pending = sheet.Range(top, 1, top, LastColumn).Merge();
                pending.Value = "Todavía no hay probetas rotas: el resumen y el gráfico aparecen con los primeros resultados.";
                pending.Style.Font.FontSize = 8.5;
                return top;
            }

            Heading(sheet, top, "Resumen por edad");
            var row = top + 1;
            var headings = new[] { "Edad (días)", "Probetas", "Mínima (kg/cm²)", "Media (kg/cm²)", "Media % fck" };
            for (var i = 0; i < headings.Length; i++)
            {
                sheet.Cell(row, i + 1).Value = headings[i];
            }

            var heading = sheet.Range(row, 1, row, headings.Length);
            heading.Style.Font.Bold = true;
            heading.Style.Fill.BackgroundColor = HeadingFill;
            heading.Style.Alignment.WrapText = true;
            heading.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
            sheet.Row(row).Height = 27;

            var fck = data.Test.ConcreteDesignFck;
            foreach (var age in tested.GroupBy(s => s.AgeDays).OrderBy(g => g.Key))
            {
                row++;
                var strengths = age.Select(s => s.StrengthKgPerCm2.Value).ToList();
                var average = strengths.Average();
                sheet.Cell(row, 1).Value = age.Key;
                sheet.Cell(row, 2).Value = strengths.Count;
                sheet.Cell(row, 3).Value = strengths.Min();
                sheet.Cell(row, 4).Value = average;
                Set(sheet.Cell(row, 5), SpecimenMath.PercentOfFck(average, fck));
                sheet.Range(row, 1, row, 2).Style.NumberFormat.Format = "#,##0";
                sheet.Range(row, 3, row, 4).Style.NumberFormat.Format = "#,##0.00";
                sheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                sheet.Range(row, 1, row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            Grid(sheet.Range(top + 1, 1, row, headings.Length));
            row++;
            var counts = sheet.Range(row, 1, row, 5).Merge();
            counts.Value = AppCulture.Format("Probetas rotas: {0:#,##0} de {1:#,##0}.", tested.Count, data.Specimens.Count);
            counts.Style.Font.FontSize = 8.5;

            // The report's chart, as a picture (Excel charts can't be written with ClosedXML).
            const double ChartWidthMm = 140;
            const double ChartHeightMm = 62;
            var chart = StrengthChart.RenderPng(tested.Select(s => (s.AgeDays, s.StrengthKgPerCm2.Value)).ToList(), fck, ChartWidthMm, ChartHeightMm);
            var picture = AddPicture(sheet, chart, sheet.Cell(top + 1, 7), ChartWidthMm, ChartHeightMm);
            var chartRows = picture == null ? 0 : (int)Math.Ceiling(picture.Height / 20.0);
            return Math.Max(row, top + chartRows);
        }

        /// <summary>The report's note (A:I) and the stamp (J:M), when there are any.</summary>
        private static void AddNoteAndStamp(IXLWorksheet sheet, ReportBranding branding, int top)
        {
            var noteText = branding?.Settings.NoteText;
            if (!string.IsNullOrWhiteSpace(noteText))
            {
                // About 110 characters fit on a line of A:I; merged cells need their height set by hand.
                var lines = noteText.Replace("\r\n", "\n").Split('\n').Sum(line => Math.Max(1, (line.Length + 109) / 110));
                var bottom = top + Math.Max(0, lines - 1);
                var note = sheet.Range(top, 1, bottom, 9).Merge();
                note.Value = noteText;
                note.Style.Font.FontSize = 8.5;
                note.Style.Alignment.WrapText = true;
                note.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }

            AddPicture(sheet, branding?.Stamp, sheet.Cell(top, 10), 60, 32);
        }

        private static void SetUpPrinting(IXLWorksheet sheet, ReportBranding branding, int headingRow)
        {
            var setup = sheet.PageSetup;
            setup.PageOrientation = XLPageOrientation.Landscape;
            setup.PaperSize = branding?.Settings.PaperSize == ReportPaperSize.Letter ? XLPaperSize.LetterPaper : XLPaperSize.A4Paper;
            setup.FitToPages(1, 0);
            setup.Margins.Left = setup.Margins.Right = 0.4;
            setup.Margins.Top = setup.Margins.Bottom = 0.5;
            setup.SetRowsToRepeatAtTop(headingRow, headingRow);
            var footer = branding?.Settings.FooterText;
            if (!string.IsNullOrWhiteSpace(footer))
            {
                setup.Footer.Center.AddText(footer);
            }

            setup.Footer.Right.AddText("Hoja ");
            setup.Footer.Right.AddText(XLHFPredefinedText.PageNumber);
            setup.Footer.Right.AddText(" de ");
            setup.Footer.Right.AddText(XLHFPredefinedText.NumberOfPages);
        }

        private static void Heading(IXLWorksheet sheet, int row, string text)
        {
            var cell = sheet.Cell(row, 1);
            cell.Value = text;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            sheet.Row(row).Height = 20;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
        }

        private static void Grid(IXLRange range)
        {
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorderColor = Rule;
            range.Style.Border.InsideBorderColor = Rule;
        }

        /// <summary>
        /// Grouped, with only the decimals the value has ("210", "17.5", "1,225.75"): Excel would print "210."
        /// with "#,##0.##".
        /// </summary>
        private static string Flexible(decimal value, string suffix = "")
        {
            var decimals = 0;
            while (decimals < 3 && value != Math.Round(value, decimals))
            {
                decimals++;
            }

            return AppCulture.Pattern(decimals) + suffix;
        }

        /// <summary>An image fitted in a box (mm), keeping its proportions; nothing for missing or unreadable bytes.</summary>
        private static ClosedXML.Excel.Drawings.IXLPicture AddPicture(IXLWorksheet sheet, byte[] bytes, IXLCell at, double maxWidthMm, double maxHeightMm)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            double width, height;
            try
            {
                using (var stream = new MemoryStream(bytes))
                using (var image = System.Drawing.Image.FromStream(stream, false, false))
                {
                    width = image.Width / Math.Max(1, image.HorizontalResolution);
                    height = image.Height / Math.Max(1, image.VerticalResolution);
                }
            }
            catch (ArgumentException)
            {
                return null;
            }

            // Inches → mm, fitted, then Excel's pixels (96 per inch).
            var scale = Math.Min(maxWidthMm / (width * 25.4), maxHeightMm / (height * 25.4));
            var pixelsPerMm = 96 / 25.4;
            var picture = sheet.AddPicture(new MemoryStream(bytes));
            picture.MoveTo(at, 3, 3);
            picture.WithSize(
                Math.Max(1, (int)Math.Round(width * 25.4 * scale * pixelsPerMm)),
                Math.Max(1, (int)Math.Round(height * 25.4 * scale * pixelsPerMm)));
            return picture;
        }

        private static void Set(IXLCell cell, decimal? value)
        {
            if (value.HasValue)
            {
                cell.Value = value.Value;
            }
        }

        private static void Set(IXLCell cell, double? value)
        {
            if (value.HasValue)
            {
                cell.Value = value.Value;
            }
        }
    }
}
