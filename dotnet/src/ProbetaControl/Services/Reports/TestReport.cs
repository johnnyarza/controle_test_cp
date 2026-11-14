using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using ProbetaControl.Models;

namespace ProbetaControl.Services.Reports
{
    /// <summary>What the ensayo report prints: the test, its design and the chosen probetas.</summary>
    public sealed class TestReportData
    {
        public CompressionTest Test { get; set; }

        /// <summary>With its materials; null if it could not be read.</summary>
        public ConcreteDesign Design { get; set; }

        /// <summary>Printed in this order (archived ones are left out by the caller).</summary>
        public IReadOnlyList<Specimen> Specimens { get; set; }

        public ReportBranding Branding { get; set; }

        public DateTime PrintedOn { get; set; }
    }

    /// <summary>
    /// "Rotura a compresión simple de probetas cilíndricas de hormigón" (landscape): test data and mix design,
    /// the probetas table, a summary by age with the strength chart, then the note and the stamp.
    /// </summary>
    public static class TestReport
    {
        public const string Title = "Rotura a compresión simple de probetas cilíndricas de hormigón";

        public static Document Build(TestReportData data)
        {
            var test = data.Test;
            var layout = ReportLayout.Create(Title, data.Branding, landscape: true, data.PrintedOn, new[]
            {
                string.Format(CultureInfo.InvariantCulture, "Ensayo Nº {0}", test.Id),
                "Fecha: " + AppCulture.FormatDate(test.TestDate),
            });

            AddTestData(layout, data);
            AddResults(layout, data);
            AddSummary(layout, data);
            layout.AddNoteAndStamp(data.Branding);
            return layout.Document;
        }

        /// <summary>Client, provider, project and design on the left; the mix (materials and kg/m³) on the right.</summary>
        private static void AddTestData(ReportLayout layout, TestReportData data)
        {
            var test = data.Test;
            var design = data.Design;
            var mixWidth = Math.Min(118, layout.ContentWidthMm * 0.45);
            const double GapMm = 6;
            var dataWidth = layout.ContentWidthMm - mixWidth - GapMm;

            var outer = layout.Section.AddTable();
            outer.AddColumn(Unit.FromMillimeter(dataWidth));
            outer.AddColumn(Unit.FromMillimeter(GapMm));
            outer.AddColumn(Unit.FromMillimeter(mixWidth));
            outer.LeftPadding = outer.RightPadding = 0;
            var row = outer.AddRow();

            var fields = ReportLayout.AddFieldTable(row.Cells[0].Elements.AddTable(), 40, dataWidth - 40);
            ReportLayout.AddField(fields,"Cliente", test.ClientName);
            ReportLayout.AddField(fields,"Proveedor del hormigón", test.ConcreteProviderName);
            ReportLayout.AddField(fields,"Proyecto / obra", test.ProjectName);
            ReportLayout.AddField(fields,"Ubicación", test.Address);
            ReportLayout.AddField(fields,"Dosificación", test.ConcreteDesignDescription);
            ReportLayout.AddField(fields,"Resistencia de diseño (fck)",
                string.Format(CultureInfo.InvariantCulture, "{0:0.##} kg/cm²", test.ConcreteDesignFck));
            ReportLayout.AddField(fields,"Slump de diseño", SlumpText(design?.Slump));
            var ratio = design?.WaterCementRatio;
            if (ratio.HasValue)
            {
                ReportLayout.AddField(fields,"Relación agua/cemento", ratio.Value.ToString("0.00", CultureInfo.InvariantCulture));
            }

            var mixCell = row.Cells[2];
            var materials = design?.UsedMaterials ?? Array.Empty<ConcreteDesignMaterial>();
            var caption = mixCell.AddParagraph("Dosificación por m³ de hormigón");
            caption.Format.Font.Bold = true;
            caption.Format.SpaceAfter = Unit.FromPoint(2);
            if (materials.Count == 0)
            {
                mixCell.AddParagraph("Sin materiales registrados.").Style = "Small";
                return;
            }

            var mix = mixCell.Elements.AddTable();
            mix.Style = "TableText";
            mix.Borders.Color = ReportLayout.Rule;
            mix.Borders.Width = 0.5;
            mix.TopPadding = mix.BottomPadding = Unit.FromPoint(1.2);
            mix.LeftPadding = mix.RightPadding = Unit.FromPoint(3);
            foreach (var width in new[] { 7.0, mixWidth * 0.42, mixWidth * 0.33 })
            {
                mix.AddColumn(Unit.FromMillimeter(width));
            }

            mix.AddColumn(Unit.FromMillimeter(mixWidth - 7 - mixWidth * 0.75)).Format.Alignment = ParagraphAlignment.Right;
            var heading = mix.AddRow();
            heading.Shading.Color = ReportLayout.HeadingFill;
            heading.Format.Font.Bold = true;
            heading.Cells[0].AddParagraph("#");
            heading.Cells[1].AddParagraph("Material");
            heading.Cells[2].AddParagraph("Proveedor");
            heading.Cells[3].AddParagraph("kg/m³");
            var position = 1;
            foreach (var material in materials)
            {
                ReportLayout.AddDataRow(mix,
                    position++.ToString(CultureInfo.InvariantCulture),
                    material.MaterialName,
                    material.ProviderName,
                    ReportLayout.Number(material.QuantityKgPerM3, "0.###"));
            }

            var total = mix.AddRow();
            total.Format.Font.Bold = true;
            total.Cells[0].MergeRight = 2;
            total.Cells[0].AddParagraph("Total");
            total.Cells[3].AddParagraph(design.TotalKgPerM3.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static void AddResults(ReportLayout layout, TestReportData data)
        {
            layout.AddHeading(data.Specimens.Count == 1 ? "Resultados (1 probeta)" : $"Resultados ({data.Specimens.Count} probetas)");
            if (data.Specimens.Count == 0)
            {
                layout.Section.AddParagraph("El ensayo no tiene probetas para imprimir.");
                return;
            }

            var table = layout.AddDataTable(new (string, double, bool)[]
            {
                ("Nº", 6, true),
                ("Descripción", 20, false),
                ("Slump (cm)", 7, true),
                ("Moldeo", 10, false),
                ("Rotura", 10, false),
                ("Edad (días)", 7, true),
                ("Ø (cm)", 7, true),
                ("Altura (cm)", 7, true),
                ("Peso (kg)", 7, true),
                ("Densidad (kg/m³)", 9, true),
                ("Carga (t)", 7, true),
                ("Resistencia (kg/cm²)", 10, true),
                ("% fck", 6, true),
            });

            var fck = data.Test.ConcreteDesignFck;
            foreach (var s in data.Specimens)
            {
                var strength = s.StrengthKgPerCm2;
                ReportLayout.AddDataRow(table,
                    s.Id.ToString(CultureInfo.InvariantCulture),
                    s.Code,
                    ReportLayout.Number(s.Slump, "0.#"),
                    AppCulture.FormatDate(s.MoldDate),
                    AppCulture.FormatDate(s.RuptureDate),
                    s.AgeDays.ToString(CultureInfo.InvariantCulture),
                    ReportLayout.Number(s.DiameterCm, "0.00"),
                    ReportLayout.Number(s.HeightCm, "0.00"),
                    ReportLayout.Number(s.WeightKg, "0.000"),
                    ReportLayout.Number(s.DensityKgPerM3, "0"),
                    s.IsTested ? ReportLayout.Number(s.RuptureLoadTon, "0.00") : "—",
                    s.IsTested ? ReportLayout.Number(strength, "0.00") : "—",
                    ReportLayout.Number(SpecimenMath.PercentOfFck(strength, fck), "0"));
            }
        }

        /// <summary>Averages per age (7, 14, 28 days…) next to the chart; kept on one page.</summary>
        private static void AddSummary(ReportLayout layout, TestReportData data)
        {
            var tested = data.Specimens.Where(s => s.StrengthKgPerCm2.HasValue).ToList();
            if (tested.Count == 0)
            {
                var pending = layout.Section.AddParagraph("Todavía no hay probetas rotas: el resumen y el gráfico aparecen con los primeros resultados.");
                pending.Style = "Small";
                pending.Format.SpaceBefore = Unit.FromMillimeter(2);
                return;
            }

            layout.AddHeading("Resumen por edad");
            const double TableMm = 112;
            const double GapMm = 8;
            var chartMm = Math.Min(140, layout.ContentWidthMm - TableMm - GapMm);

            var outer = layout.Section.AddTable();
            outer.LeftPadding = outer.RightPadding = 0;
            outer.AddColumn(Unit.FromMillimeter(TableMm));
            outer.AddColumn(Unit.FromMillimeter(GapMm));
            outer.AddColumn(Unit.FromMillimeter(chartMm));
            var row = outer.AddRow();

            var fck = data.Test.ConcreteDesignFck;
            var byAge = row.Cells[0].Elements.AddTable();
            byAge.Style = "TableText";
            byAge.Borders.Color = ReportLayout.Rule;
            byAge.Borders.Width = 0.5;
            byAge.TopPadding = byAge.BottomPadding = Unit.FromPoint(1.5);
            byAge.LeftPadding = byAge.RightPadding = Unit.FromPoint(3);
            foreach (var width in new[] { 18.0, 18, 26, 26, 24 })
            {
                byAge.AddColumn(Unit.FromMillimeter(width)).Format.Alignment = ParagraphAlignment.Right;
            }

            var heading = byAge.AddRow();
            heading.Shading.Color = ReportLayout.HeadingFill;
            heading.Format.Font.Bold = true;
            heading.VerticalAlignment = VerticalAlignment.Bottom;
            heading.Cells[0].AddParagraph("Edad (días)");
            heading.Cells[1].AddParagraph("Probetas");
            heading.Cells[2].AddParagraph("Mínima (kg/cm²)");
            heading.Cells[3].AddParagraph("Media (kg/cm²)");
            heading.Cells[4].AddParagraph("Media % fck");
            foreach (var age in tested.GroupBy(s => s.AgeDays).OrderBy(g => g.Key))
            {
                var strengths = age.Select(s => s.StrengthKgPerCm2.Value).ToList();
                var average = strengths.Average();
                ReportLayout.AddDataRow(byAge,
                    age.Key.ToString(CultureInfo.InvariantCulture),
                    strengths.Count.ToString(CultureInfo.InvariantCulture),
                    strengths.Min().ToString("0.00", CultureInfo.InvariantCulture),
                    average.ToString("0.00", CultureInfo.InvariantCulture),
                    ReportLayout.Number(SpecimenMath.PercentOfFck(average, fck), "0"));
            }

            var counts = row.Cells[0].AddParagraph(string.Format(CultureInfo.InvariantCulture,
                "Probetas rotas: {0} de {1}.", tested.Count, data.Specimens.Count));
            counts.Style = "Small";
            counts.Format.SpaceBefore = Unit.FromPoint(4);

            var chart = StrengthChart.RenderPng(tested.Select(s => (s.AgeDays, s.StrengthKgPerCm2.Value)).ToList(), fck, chartMm, 62);
            var image = row.Cells[2].AddParagraph().AddImage("base64:" + Convert.ToBase64String(chart));
            image.LockAspectRatio = false;
            image.Width = Unit.FromMillimeter(chartMm);
            image.Height = Unit.FromMillimeter(62);
        }

        /// <summary>"8 ± 1 cm" (legacy tolerance rule), "—" without a slump.</summary>
        public static string SlumpText(decimal? slump)
        {
            if (!slump.HasValue || slump.Value <= 0)
            {
                return "—";
            }

            var tolerance = Calculators.SlumpToleranceCm(slump.Value);
            return tolerance.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "{0:0.#} ± {1} cm", slump.Value, tolerance.Value)
                : string.Format(CultureInfo.InvariantCulture, "{0:0.#} cm", slump.Value);
        }
    }
}
