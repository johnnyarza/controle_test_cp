using System;
using System.Globalization;
using ClosedXML.Excel;
using ProbetaControl.Models;

namespace ProbetaControl.Services.Reports
{
    /// <summary>
    /// The ensayo as an Excel workbook, for the lab's own calculations. Sheet "Probetas": one row per probeta
    /// with real numbers and dates (heading in row 1, ready to filter and sort); sheet "Ensayo": the test's data
    /// and its mix. Same content as the ensayo report (<see cref="TestReportData"/>).
    /// </summary>
    public static class TestExcelExport
    {
        public const string FileFilter = "Libro de Excel (*.xlsx)|*.xlsx";
        public const string SpecimensSheet = "Probetas";
        public const string TestSheet = "Ensayo";

        /// <summary>Excel's own date format: shows as dd/mm/yyyy whatever the PC's regional settings.</summary>
        private const string DateFormat = "dd/mm/yyyy";

        private static readonly XLColor HeadingFill = XLColor.FromArgb(0xE8, 0xEC, 0xF1);

        /// <summary>Column headings of the probetas sheet, with their widths (characters) and number formats.</summary>
        private static readonly (string Heading, double Width, string Format)[] Columns =
        {
            ("Nº", 9, "0"),
            ("Descripción", 28, null),
            ("Slump (cm)", 11, "0.0"),
            ("Moldeo", 12, DateFormat),
            ("Rotura", 12, DateFormat),
            ("Edad (días)", 11, "0"),
            ("Ø (cm)", 9, "0.00"),
            ("Altura (cm)", 11, "0.00"),
            ("Peso (kg)", 11, "0.000"),
            ("Densidad (kg/m³)", 16, "0"),
            ("Carga (t)", 10, "0.00"),
            ("Resistencia (kg/cm²)", 19, "0.00"),
            ("% fck", 8, "0"),
        };

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
            AddSpecimens(workbook.AddWorksheet(SpecimensSheet), data);
            AddTest(workbook.AddWorksheet(TestSheet), data);
            return workbook;
        }

        private static void AddSpecimens(IXLWorksheet sheet, TestReportData data)
        {
            for (var i = 0; i < Columns.Length; i++)
            {
                var column = sheet.Column(i + 1);
                column.Width = Columns[i].Width;
                if (Columns[i].Format != null)
                {
                    column.Style.NumberFormat.Format = Columns[i].Format;
                }

                sheet.Cell(1, i + 1).Value = Columns[i].Heading;
            }

            var heading = sheet.Range(1, 1, 1, Columns.Length);
            heading.Style.Font.Bold = true;
            heading.Style.Fill.BackgroundColor = HeadingFill;
            heading.Style.Alignment.WrapText = true;
            heading.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
            sheet.SheetView.FreezeRows(1);

            var fck = data.Test.ConcreteDesignFck;
            var rowNumber = 2;
            foreach (var s in data.Specimens)
            {
                var row = sheet.Row(rowNumber++);
                row.Cell(1).Value = s.Id;
                row.Cell(2).Value = s.Code ?? string.Empty;
                Set(row.Cell(3), s.Slump);
                row.Cell(4).Value = s.MoldDate.Date;
                row.Cell(5).Value = s.RuptureDate.Date;
                row.Cell(6).Value = s.AgeDays;
                row.Cell(7).Value = s.DiameterCm;
                row.Cell(8).Value = s.HeightCm;
                row.Cell(9).Value = s.WeightKg;
                Set(row.Cell(10), s.DensityKgPerM3);

                // Not broken yet: the cells stay empty, so averages in Excel skip them (the legacy file had 0).
                if (s.IsTested)
                {
                    Set(row.Cell(11), s.RuptureLoadTon);
                    Set(row.Cell(12), s.StrengthKgPerCm2);
                    Set(row.Cell(13), SpecimenMath.PercentOfFck(s.StrengthKgPerCm2, fck));
                }
            }

            // Filter arrows on the heading; with no probetas there is nothing to filter.
            if (data.Specimens.Count > 0)
            {
                sheet.Range(1, 1, rowNumber - 1, Columns.Length).SetAutoFilter();
            }
        }

        private static void AddTest(IXLWorksheet sheet, TestReportData data)
        {
            var test = data.Test;
            var design = data.Design;
            sheet.Column(1).Width = 28;
            sheet.Column(2).Width = 34;
            sheet.Column(3).Width = 14;

            var row = 1;
            void Field(string label, XLCellValue value, string format = null)
            {
                sheet.Cell(row, 1).Value = label;
                sheet.Cell(row, 1).Style.Font.Bold = true;
                var cell = sheet.Cell(row, 2);
                cell.Value = value;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                if (format != null)
                {
                    cell.Style.NumberFormat.Format = format;
                }

                row++;
            }

            Field("Ensayo Nº", test.Id);
            Field("Fecha", test.TestDate.Date, DateFormat);
            Field("Cliente", test.ClientName ?? string.Empty);
            Field("Proveedor del hormigón", test.ConcreteProviderName ?? string.Empty);
            Field("Proyecto / obra", test.ProjectName ?? string.Empty);
            Field("Ubicación", test.Address ?? string.Empty);
            Field("Dosificación", test.ConcreteDesignDescription ?? string.Empty);
            Field("fck (kg/cm²)", test.ConcreteDesignFck);
            Field("Slump de diseño (cm)", design?.Slump.HasValue == true ? design.Slump.Value : XLCellValue.FromObject(null));
            var ratio = design?.WaterCementRatio;
            Field("Relación agua/cemento", ratio.HasValue ? Math.Round(ratio.Value, 2) : XLCellValue.FromObject(null), "0.00");
            Field("Exportado", data.PrintedOn.Date, DateFormat);

            var materials = design?.UsedMaterials;
            if (materials == null || materials.Count == 0)
            {
                return;
            }

            row++;
            sheet.Cell(row, 1).Value = "Material";
            sheet.Cell(row, 2).Value = "Proveedor";
            sheet.Cell(row, 3).Value = "kg/m³";
            var heading = sheet.Range(row, 1, row, 3);
            heading.Style.Font.Bold = true;
            heading.Style.Fill.BackgroundColor = HeadingFill;
            row++;
            foreach (var material in materials)
            {
                sheet.Cell(row, 1).Value = material.MaterialName ?? string.Empty;
                sheet.Cell(row, 2).Value = material.ProviderName ?? string.Empty;
                Set(sheet.Cell(row, 3), material.QuantityKgPerM3);
                row++;
            }

            sheet.Cell(row, 1).Value = "Total";
            sheet.Cell(row, 3).Value = design.TotalKgPerM3;
            sheet.Range(row, 1, row, 3).Style.Font.Bold = true;
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
