using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using ProbetaControl.Models;

namespace ProbetaControl.Services.Reports
{
    /// <summary>What the report by client prints: the client and the probetas of all their ensayos.</summary>
    public sealed class ClientReportData
    {
        public Client Client { get; set; }

        /// <summary>Only ensayos dated from this day on (null = from the first one).</summary>
        public DateTime? TestDateFrom { get; set; }

        /// <summary>Only ensayos dated up to this day (null = up to the last one).</summary>
        public DateTime? TestDateTo { get; set; }

        /// <summary>Active probetas of the client's active ensayos, oldest ensayo first.</summary>
        public IReadOnlyList<ClientSpecimen> Specimens { get; set; }

        /// <summary>True when the client has more probetas than the report holds (<see cref="ReportService.MaxRows"/>).</summary>
        public bool IsCutShort { get; set; }

        public ReportBranding Branding { get; set; }

        public DateTime PrintedOn { get; set; }
    }

    /// <summary>
    /// "Detalle de roturas por cliente" (landscape): the client, then one row per probeta of every ensayo of
    /// theirs under a line for its ensayo, and the note and stamp at the end.
    /// </summary>
    public static class ClientReport
    {
        public const string Heading = "Detalle de roturas por cliente";

        public static Document Build(ClientReportData data)
        {
            var layout = ReportLayout.Create(TestReport.Title, data.Branding, landscape: true, data.PrintedOn);
            AddClient(layout, data);
            AddResults(layout, data);
            layout.AddNoteAndStamp(data.Branding);
            return layout.Document;
        }

        /// <summary>"Informe de Constructora Andina 2026-11-13" for "Guardar PDF…".</summary>
        public static string FileName(Client client, DateTime printedOn) =>
            string.Format(CultureInfo.InvariantCulture, "Informe de {0} {1:yyyy-MM-dd}", client.Name, printedOn);

        /// <summary>"01/10/2026 – 31/10/2026", "desde 01/10/2026", "hasta 31/10/2026" or "Todos los ensayos" (dates of the ensayos).</summary>
        public static string PeriodText(DateTime? from, DateTime? to) =>
            from.HasValue && to.HasValue ? AppCulture.FormatDate(from) + " – " + AppCulture.FormatDate(to)
            : from.HasValue ? "desde " + AppCulture.FormatDate(from)
            : to.HasValue ? "hasta " + AppCulture.FormatDate(to)
            : "Todos los ensayos";

        private static void AddClient(ReportLayout layout, ClientReportData data)
        {
            var client = data.Client;
            var fields = ReportLayout.AddFieldTable(layout.Section.AddTable(), 40, layout.ContentWidthMm - 40);
            ReportLayout.AddField(fields, "Cliente", client.Name);
            ReportLayout.AddField(fields, "Dirección", client.Address);
            ReportLayout.AddField(fields, "Teléfono", client.Phone);
            ReportLayout.AddField(fields, "Período", PeriodText(data.TestDateFrom, data.TestDateTo));

            var tests = data.Specimens.Select(s => s.CompressionTestId).Distinct().Count();
            var tested = data.Specimens.Count(s => s.IsTested);
            ReportLayout.AddField(fields, "Ensayos", tests.ToString(CultureInfo.InvariantCulture));
            ReportLayout.AddField(fields, "Probetas", string.Format(CultureInfo.InvariantCulture,
                "{0} ({1} rotas)", data.Specimens.Count, tested));
        }

        /// <summary>
        /// "Ensayo Nº 12 · 01/10/2026 · Torre Norte · fck 210 kg/cm²" across the table, kept on the same page
        /// as the first probeta under it.
        /// </summary>
        private static void AddTestRow(Table table, ClientSpecimen first)
        {
            var row = table.AddRow();
            row.KeepWith = 1;
            row.Shading.Color = ReportLayout.HeadingFill;
            row.Cells[0].MergeRight = table.Columns.Count - 1;
            row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            var text = row.Cells[0].AddParagraph(string.Format(CultureInfo.InvariantCulture,
                "Ensayo Nº {0} · {1} · {2} · fck {3:0.##} kg/cm²",
                first.CompressionTestId, AppCulture.FormatDate(first.TestDate), first.ProjectName, first.ConcreteDesignFck));
            text.Format.Font.Bold = true;
        }

        private static void AddResults(ReportLayout layout, ClientReportData data)
        {
            layout.AddHeading(Heading);
            if (data.Specimens.Count == 0)
            {
                layout.Section.AddParagraph(data.TestDateFrom.HasValue || data.TestDateTo.HasValue
                    ? "El cliente no tiene probetas en ensayos de ese período."
                    : "El cliente no tiene probetas en sus ensayos.");
                return;
            }

            var table = layout.AddDataTable(new (string, double, bool)[]
            {
                ("Nº", 6, true),
                ("Descripción", 20, false),
                ("Slump (cm)", 6, true),
                ("Moldeo", 9, false),
                ("Rotura", 9, false),
                ("Edad (días)", 6, true),
                ("Ø (cm)", 6, true),
                ("Altura (cm)", 6.5, true),
                ("Peso (kg)", 6.5, true),
                ("Densidad (kg/m³)", 8, true),
                ("Carga (t)", 6.5, true),
                ("Resistencia (kg/cm²)", 9.5, true),
                ("% fck", 5.5, true),
            });

            var testId = 0;
            foreach (var s in data.Specimens)
            {
                if (s.CompressionTestId != testId)
                {
                    testId = s.CompressionTestId;
                    AddTestRow(table, s);
                }

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
                    ReportLayout.Number(SpecimenMath.PercentOfFck(strength, s.ConcreteDesignFck), "0"));
            }

            if (data.IsCutShort)
            {
                // The last ensayo may be incomplete, so the note names what was printed.
                var cut = layout.Section.AddParagraph(string.Format(CultureInfo.InvariantCulture,
                    "El informe se cortó en las primeras {0} probetas (las de los ensayos más antiguos).", data.Specimens.Count));
                cut.Style = "Small";
                cut.Format.SpaceBefore = Unit.FromMillimeter(2);
            }
        }
    }
}
