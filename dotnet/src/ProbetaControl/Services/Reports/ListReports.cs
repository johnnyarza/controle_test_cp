using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using ProbetaControl.Models;

namespace ProbetaControl.Services.Reports
{
    /// <summary>What a list report prints: the rows the list page's search finds, not only the ones on screen.</summary>
    public sealed class ListReportData<T>
    {
        public IReadOnlyList<T> Items { get; set; }

        /// <summary>Rows matching the search; more than <see cref="Items"/> when the report was cut short.</summary>
        public int TotalCount { get; set; }

        /// <summary>The page's search text, printed under the heading (null or blank = everything).</summary>
        public string SearchText { get; set; }

        /// <summary>"Mostrar archivados" was on: archived rows are printed too, marked as such.</summary>
        public bool IncludeArchived { get; set; }

        public ReportBranding Branding { get; set; }

        public DateTime PrintedOn { get; set; }
    }

    /// <summary>
    /// "Listado de …" (portrait): the list page's records, in the page's order. Clientes, Proveedores (same
    /// columns) and Materiales are one table; Dosificaciones are a block each, with their materials.
    /// </summary>
    public static class ListReports
    {
        public const string ClientsTitle = "Listado de clientes";
        public const string ProvidersTitle = "Listado de proveedores";
        public const string MaterialsTitle = "Listado de materiales";
        public const string DesignsTitle = "Listado de dosificaciones";

        private const string ArchivedMark = " (archivado)";

        public static Document BuildClients(ListReportData<Client> data) => BuildContacts(ClientsTitle, new Noun("Ningún cliente", "1 cliente", "clientes"), data);

        public static Document BuildProviders(ListReportData<Provider> data) => BuildContacts(ProvidersTitle, new Noun("Ningún proveedor", "1 proveedor", "proveedores"), data);

        public static Document BuildMaterials(ListReportData<Material> data) => Build(MaterialsTitle, new Noun("Ningún material", "1 material", "materiales"), data,
            new (string, double, bool)[]
            {
                ("Nº", 7, true),
                ("Nombre", 40, false),
                ("Tipo", 16, false),
                ("Proveedor", 37, false),
            },
            m => new[]
            {
                Id(m.Id),
                Name(m.Name, m.IsArchived),
                m.TypeLabel,
                m.ProviderName,
            });

        /// <summary>
        /// One block per dosificación, like the materials panel of the screen: a shaded line with the name, Nº,
        /// fck, slump and a/c, then its materials (#, material, provider, type, kg/m³) and the total.
        /// A block is never split between two pages.
        /// </summary>
        public static Document BuildDesigns(ListReportData<ConcreteDesign> data) =>
            Build(DesignsTitle, new Noun("Ninguna dosificación", "1 dosificación", "dosificaciones"), data, layout =>
            {
                foreach (var design in data.Items)
                {
                    AddDesignBlock(layout, design);
                }
            });

        /// <summary>"Listado de clientes" → "Listado de clientes 2026-11-13" for "Guardar PDF…".</summary>
        public static string FileName(string title, DateTime printedOn) =>
            string.Format(CultureInfo.InvariantCulture, "{0} {1:yyyy-MM-dd}", title, printedOn);

        private static Document BuildContacts<T>(string title, Noun noun, ListReportData<T> data)
            where T : ContactRecord => Build(title, noun, data,
            new (string, double, bool)[]
            {
                ("Nº", 7, true),
                ("Nombre", 29, false),
                ("Teléfono", 13, false),
                ("Correo electrónico", 26, false),
                ("Dirección", 25, false),
            },
            c => new[]
            {
                Id(c.Id),
                Name(c.Name, c.IsArchived),
                c.Phone,
                c.Email,
                c.Address,
            });

        /// <summary>A list as one table: a row per record.</summary>
        private static Document Build<T>(string title, Noun noun, ListReportData<T> data,
            IReadOnlyList<(string Heading, double Weight, bool Right)> columns, Func<T, string[]> cells)
        {
            return Build(title, noun, data, layout =>
            {
                var table = layout.AddDataTable(columns);
                foreach (var item in data.Items)
                {
                    ReportLayout.AddDataRow(table, cells(item));
                }
            });
        }

        /// <summary>The summary line, the records (<paramref name="addItems"/>, skipped when there are none) and the "cut short" note.</summary>
        private static Document Build<T>(string title, Noun noun, ListReportData<T> data, Action<ReportLayout> addItems)
        {
            var layout = ReportLayout.Create(title, data.Branding, landscape: false, data.PrintedOn);

            var summary = layout.Section.AddParagraph(Summary(noun, data));
            summary.Format.SpaceAfter = Unit.FromMillimeter(2);

            if (data.Items.Count == 0)
            {
                return layout.Document;
            }

            addItems(layout);

            if (data.TotalCount > data.Items.Count)
            {
                var cut = layout.Section.AddParagraph(string.Format(CultureInfo.InvariantCulture,
                    "El listado se cortó en {0} de {1} {2}. Use la búsqueda de la página para imprimir una parte.",
                    data.Items.Count, data.TotalCount, noun.Plural));
                cut.Style = "Small";
                cut.Format.SpaceBefore = Unit.FromMillimeter(2);
            }

            return layout.Document;
        }

        private static void AddDesignBlock(ReportLayout layout, ConcreteDesign design)
        {
            var width = layout.ContentWidthMm;
            var table = layout.Section.AddTable();
            table.Style = "TableText";
            table.Borders.Color = ReportLayout.Rule;
            table.Borders.Width = 0.5;
            table.TopPadding = table.BottomPadding = Unit.FromPoint(1.5);
            table.LeftPadding = table.RightPadding = Unit.FromPoint(3);
            const double NumberMm = 8;
            foreach (var (mm, right) in new[]
                     {
                         (NumberMm, true), (width * 0.36, false), (width * 0.28, false), (width * 0.14, false), (width * 0.22 - NumberMm, true),
                     })
            {
                table.AddColumn(Unit.FromMillimeter(mm)).Format.Alignment = right ? ParagraphAlignment.Right : ParagraphAlignment.Left;
            }

            var title = table.AddRow();
            title.Shading.Color = ReportLayout.HeadingFill;
            title.VerticalAlignment = VerticalAlignment.Center;
            title.Cells[0].MergeRight = 2;
            title.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            var name = title.Cells[0].AddParagraph();
            name.Format.Font.Size = 9;
            name.AddFormattedText(Name(design.Description, design.IsArchived), TextFormat.Bold);
            name.AddFormattedText("   Nº " + Id(design.Id), new Font { Color = ReportLayout.Muted, Size = 7.5 });
            title.Cells[3].MergeRight = 1;
            title.Cells[3].Format.Alignment = ParagraphAlignment.Right;
            var figures = title.Cells[3].AddParagraph(string.Format(CultureInfo.InvariantCulture, "fck {0} kg/cm²  ·  Slump {1}  ·  a/c {2}",
                ReportLayout.Number(design.Fck, "0.##"),
                design.Slump.HasValue ? ReportLayout.Number(design.Slump, "0.#") + " cm" : "—",
                design.WaterCementRatio.HasValue ? ReportLayout.Number(design.WaterCementRatio, "0.00") : "—"));
            figures.Format.Font.Bold = true;

            var materials = design.UsedMaterials;
            if (materials.Count == 0)
            {
                var none = table.AddRow();
                none.Cells[0].MergeRight = 4;
                none.Cells[0].Format.Alignment = ParagraphAlignment.Left;
                none.Cells[0].AddParagraph("Sin materiales registrados.").Format.Font.Color = ReportLayout.Muted;
            }
            else
            {
                var heading = table.AddRow();
                heading.Format.Font.Bold = true;
                heading.Format.Font.Color = ReportLayout.Muted;
                heading.Cells[0].AddParagraph("#");
                heading.Cells[1].AddParagraph("Material");
                heading.Cells[2].AddParagraph("Proveedor");
                heading.Cells[3].AddParagraph("Tipo");
                heading.Cells[4].AddParagraph("kg/m³");

                foreach (var material in materials)
                {
                    var row = table.AddRow();
                    row.Cells[0].AddParagraph(material.Position.ToString(CultureInfo.InvariantCulture));
                    row.Cells[1].AddParagraph(material.MaterialName ?? string.Empty);
                    row.Cells[2].AddParagraph(material.ProviderName ?? string.Empty);
                    row.Cells[3].AddParagraph(MaterialTypes.Label(material.MaterialType));
                    row.Cells[4].AddParagraph(ReportLayout.Number(material.QuantityKgPerM3, "0.###"));
                }

                var total = table.AddRow();
                total.Format.Font.Bold = true;
                total.Cells[0].MergeRight = 3;
                total.Cells[0].Format.Alignment = ParagraphAlignment.Right;
                total.Cells[0].AddParagraph("Total");
                total.Cells[4].AddParagraph(ReportLayout.Number(design.TotalKgPerM3, "0.###"));
            }

            // The whole block moves to the next page when it doesn't fit (a design has at most 8 materials).
            table.Rows[0].KeepWith = table.Rows.Count - 1;

            layout.Section.AddParagraph().Format.SpaceAfter = Unit.FromMillimeter(1.5);
        }

        /// <summary>"12 clientes · Búsqueda: "andina" · Incluye archivados".</summary>
        private static string Summary<T>(Noun noun, ListReportData<T> data)
        {
            var count = Math.Max(data.TotalCount, data.Items.Count);
            var parts = new List<string>
            {
                count == 0 ? noun.None : count == 1 ? noun.One : string.Format(CultureInfo.InvariantCulture, "{0} {1}", count, noun.Plural),
            };

            if (!string.IsNullOrWhiteSpace(data.SearchText))
            {
                parts.Add("Búsqueda: \"" + data.SearchText.Trim() + "\"");
            }

            if (data.IncludeArchived)
            {
                parts.Add("Incluye archivados");
            }

            return string.Join(" · ", parts);
        }

        /// <summary>How the report counts its records: "Ninguna dosificación", "1 dosificación", "12 dosificaciones".</summary>
        private sealed class Noun
        {
            public Noun(string none, string one, string plural)
            {
                None = none;
                One = one;
                Plural = plural;
            }

            public string None { get; }

            public string One { get; }

            public string Plural { get; }
        }

        private static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);

        private static string Name(string name, bool archived) => archived ? name + ArchivedMark : name;
    }
}
