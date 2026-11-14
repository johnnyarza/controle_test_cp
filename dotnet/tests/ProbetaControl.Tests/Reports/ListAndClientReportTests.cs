using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services.Reports;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels.Catalog;
using ProbetaControl.ViewModels.CompressionTests;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.ViewModels.Reports;

namespace ProbetaControl.Tests.Reports
{
    /// <summary>Layout of the list reports and the report by client, without a database.</summary>
    public class ListAndClientReportLayoutTests
    {
        private static readonly ReportBranding Branding = new ReportBranding(new ReportSettings { LabName = "Laboratorio" }, null, null);

        private static readonly DateTime Printed = new DateTime(2026, 11, 13);

        /// <summary>The report's data table: the one whose first row is a repeated heading.</summary>
        internal static Table DataTable(Document document) =>
            document.Sections[0].Elements.OfType<Table>().Single(t => t.Rows.Count > 0 && t.Rows[0].HeadingFormat);

        /// <summary>The text of the paragraphs outside tables, one per line.</summary>
        internal static string Text(Document document) => string.Join("\n", document.Sections[0].Elements.OfType<Paragraph>()
            .Select(p => string.Concat(p.Elements.OfType<Text>().Select(t => t.Content))));

        private static string Cell(Table table, int row, int column) => ReportRenderingTests.CellText(table.Rows[row].Cells[column]);

        [Test]
        public void Client_list_is_portrait_with_one_row_per_client_and_says_what_it_shows()
        {
            var clients = Enumerable.Range(1, 120).Select(i => new Client
            {
                Id = i, Name = "Cliente " + i, Phone = "3-978-" + i, Email = $"c{i}@ejemplo.bo", Address = "Calle " + i, IsArchived = i == 2,
            }).ToList();
            var document = ListReports.BuildClients(new ListReportData<Client>
            {
                Items = clients, TotalCount = 120, SearchText = " cliente ", IncludeArchived = true, Branding = Branding, PrintedOn = Printed,
            });

            var renderer = new ReportRenderer(document);
            var size = renderer.PageSize(1);
            Assert.That(size.Width, Is.EqualTo(210 / 25.4 * 72).Within(0.5), "A4 portrait");
            Assert.That(size.Height, Is.GreaterThan(size.Width));
            Assert.That(renderer.PageCount, Is.GreaterThan(1));

            var table = DataTable(document);
            Assert.That(table.Rows.Count, Is.EqualTo(121), "heading + 120 clients");
            Assert.That(Cell(table, 1, 1), Is.EqualTo("Cliente 1"));
            Assert.That(Cell(table, 2, 1), Is.EqualTo("Cliente 2 (archivado)"));
            Assert.That(Cell(table, 1, 3), Is.EqualTo("c1@ejemplo.bo"));
            Assert.That(Text(document), Does.Contain("120 clientes · Búsqueda: \"cliente\" · Incluye archivados"));
            Assert.That(ListReports.FileName(ListReports.ClientsTitle, Printed), Is.EqualTo("Listado de clientes 2026-11-13"));
        }

        [Test]
        public void Empty_and_cut_short_lists_say_so()
        {
            var empty = ListReports.BuildDesigns(new ListReportData<ConcreteDesign>
            {
                Items = new List<ConcreteDesign>(), Branding = Branding, PrintedOn = Printed,
            });
            Assert.That(Text(empty), Does.Contain("Ninguna dosificación"));
            Assert.That(empty.Sections[0].Elements.OfType<Table>(), Is.Empty);
            Assert.That(new ReportRenderer(empty).PageCount, Is.EqualTo(1));

            var cut = ListReports.BuildProviders(new ListReportData<Provider>
            {
                Items = new List<Provider> { new Provider { Id = 1, Name = "Itacamba" } }, TotalCount = 3, Branding = Branding, PrintedOn = Printed,
            });
            Assert.That(Text(cut), Does.Contain("3 proveedores"));
            Assert.That(Text(cut), Does.Contain("El listado se cortó en 1 de 3 proveedores"));
        }

        [Test]
        public void Material_list_shows_type_and_provider()
        {
            var sample = ReportSamples.Test(null, null, 0);
            var materials = ListReports.BuildMaterials(new ListReportData<Material>
            {
                Items = new List<Material> { new Material { Id = 7, Name = "Cemento IP-30", MaterialType = MaterialTypes.Cement, ProviderName = "Itacamba" } },
                TotalCount = 1, Branding = Branding, PrintedOn = Printed,
            });
            var row = DataTable(materials).Rows[1];
            Assert.That(Enumerable.Range(0, 4).Select(i => ReportRenderingTests.CellText(row.Cells[i])),
                Is.EqualTo(new[] { "7", "Cemento IP-30", "Cemento", "Itacamba" }));
            Assert.That(Text(materials), Does.Contain("1 material"));

        }

        [Test]
        public void Design_list_is_a_block_per_dosificacion_with_its_materials_as_a_table()
        {
            var sample = ReportSamples.Test(null, null, 0).Design;
            var bare = new ConcreteDesign { Id = 9, Description = "H-13 pobre", Fck = 130, IsArchived = true };
            var document = ListReports.BuildDesigns(new ListReportData<ConcreteDesign>
            {
                Items = new List<ConcreteDesign> { sample, bare }, TotalCount = 2, IncludeArchived = true, Branding = Branding, PrintedOn = Printed,
            });

            var blocks = document.Sections[0].Elements.OfType<Table>().ToList();
            Assert.That(blocks.Count, Is.EqualTo(2), "one table per dosificación");
            Assert.That(Text(document), Does.Contain("2 dosificaciones · Incluye archivados"));

            var block = blocks[0];
            Assert.That(block.Rows.Count, Is.EqualTo(8), "title + heading + 5 materials + total");
            Assert.That(DeepText(block.Rows[0].Cells[0]), Is.EqualTo("H-21 bombeable   Nº 4"));
            Assert.That(Cell(block, 0, 3), Is.EqualTo("fck 210 kg/cm²  ·  Slump 10 cm  ·  a/c 0.53"), "185 / 350");
            Assert.That(block.Rows[0].KeepWith, Is.EqualTo(7), "the block is never split between pages");
            Assert.That(Enumerable.Range(0, 5).Select(i => Cell(block, 1, i)), Is.EqualTo(new[] { "#", "Material", "Proveedor", "Tipo", "kg/m³" }));
            Assert.That(Enumerable.Range(0, 5).Select(i => Cell(block, 2, i)), Is.EqualTo(new[] { "1", "Cemento IP-30", "Itacamba", "Cemento", "350" }));
            Assert.That(Enumerable.Range(0, 5).Select(i => Cell(block, 6, i)), Is.EqualTo(new[] { "5", "Plastificante PX-200", "Sika Bolivia", "Aditivo", "2.8" }));
            Assert.That(Cell(block, 7, 0), Is.EqualTo("Total"));
            Assert.That(Cell(block, 7, 4), Is.EqualTo("2367.8"));

            Assert.That(DeepText(blocks[1].Rows[0].Cells[0]), Is.EqualTo("H-13 pobre (archivado)   Nº 9"));
            Assert.That(Cell(blocks[1], 0, 3), Is.EqualTo("fck 130 kg/cm²  ·  Slump —  ·  a/c —"));
            Assert.That(Cell(blocks[1], 1, 0), Is.EqualTo("Sin materiales registrados."));
            Assert.That(blocks[1].Rows.Count, Is.EqualTo(2));
        }

        [Test]
        public void Design_blocks_move_whole_to_the_next_page()
        {
            var sample = ReportSamples.Test(null, null, 0).Design;
            var designs = Enumerable.Range(1, 12).Select(i => new ConcreteDesign
            {
                Id = i, Description = "H-" + (15 + i), Fck = 150 + 10 * i, Materials = sample.Materials,
            }).ToList();
            var document = ListReports.BuildDesigns(new ListReportData<ConcreteDesign>
            {
                Items = designs, TotalCount = 12, Branding = Branding, PrintedOn = Printed,
            });

            // 12 blocks of 8 rows need several pages; each block is tied to its first row.
            var renderer = new ReportRenderer(document);
            Assert.That(renderer.PageCount, Is.GreaterThan(1));
            Assert.That(document.Sections[0].Elements.OfType<Table>().All(t => t.Rows[0].KeepWith == t.Rows.Count - 1), Is.True);
        }

        /// <summary>Cell text including formatted runs (bold name, muted number).</summary>
        private static string DeepText(Cell cell) => string.Concat(cell.Elements.OfType<Paragraph>().SelectMany(p => p.Elements.Cast<DocumentObject>())
            .Select(e => e is Text text ? text.Content
                : e is FormattedText formatted ? string.Concat(formatted.Elements.OfType<Text>().Select(t => t.Content))
                : string.Empty));

        [Test]
        public void Client_report_is_landscape_with_the_probetas_under_their_ensayo()
        {
            var sample = ReportSamples.Test(null, null, 60);
            var specimens = sample.Specimens.Select((s, i) => new ClientSpecimen
            {
                Id = s.Id, CompressionTestId = 40 + i / 30, ProjectName = i < 30 ? "Torre Norte" : "Puente Sur", TestDate = new DateTime(2026, 10, 1),
                ConcreteDesignFck = 210, Code = s.Code, Slump = s.Slump, MoldDate = s.MoldDate, RuptureDate = s.RuptureDate,
                DiameterCm = s.DiameterCm, HeightCm = s.HeightCm, WeightKg = s.WeightKg, RuptureLoadTon = s.RuptureLoadTon,
            }).ToList();
            var document = ClientReport.Build(new ClientReportData
            {
                Client = new Client { Id = 3, Name = "Constructora Andina S.R.L.", Address = "Av. Bolivia 210" },
                Specimens = specimens, IsCutShort = true, Branding = sample.Branding, PrintedOn = Printed,
            });

            var size = new ReportRenderer(document).PageSize(1);
            Assert.That(size.Width, Is.GreaterThan(size.Height), "landscape");

            var table = DataTable(document);
            Assert.That(table.Columns.Count, Is.EqualTo(13), "the ensayo report's columns");
            Assert.That(table.Rows.Count, Is.EqualTo(63), "heading + 2 ensayos + 60 probetas");
            Assert.That(Cell(table, 1, 0), Is.EqualTo("Ensayo Nº 40 · 01/10/2026 · Torre Norte · fck 210 kg/cm²"));
            Assert.That(table.Rows[1].Cells[0].MergeRight, Is.EqualTo(12), "across the table");
            Assert.That(table.Rows[1].KeepWith, Is.EqualTo(1), "never alone at the bottom of a page");
            Assert.That(Cell(table, 2, 0), Is.EqualTo("5000"));
            Assert.That(Cell(table, 32, 0), Is.EqualTo("Ensayo Nº 41 · 01/10/2026 · Puente Sur · fck 210 kg/cm²"));
            Assert.That(Cell(table, 62, 10), Is.EqualTo("—"), "not broken yet");
            Assert.That(Cell(table, 2, 12), Does.Match(@"^\d+$"), "% fck of the ensayo's own design");
            Assert.That(Text(document), Does.Contain("El informe se cortó en las primeras 60 probetas"));

            var fields = document.Sections[0].Elements.OfType<Table>().First();
            Assert.That(ReportRenderingTests.CellText(fields.Rows[0].Cells[1]), Is.EqualTo("Constructora Andina S.R.L."));
            Assert.That(ReportRenderingTests.CellText(fields.Rows[3].Cells[1]), Is.EqualTo("Todos los ensayos"), "período");
            Assert.That(ReportRenderingTests.CellText(fields.Rows[4].Cells[1]), Is.EqualTo("2"), "ensayos");
            Assert.That(ReportRenderingTests.CellText(fields.Rows[5].Cells[1]), Is.EqualTo("60 (56 rotas)"));
            Assert.That(ClientReport.PeriodText(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31)), Is.EqualTo("01/10/2026 – 31/10/2026"));
            Assert.That(ClientReport.PeriodText(new DateTime(2026, 10, 1), null), Is.EqualTo("desde 01/10/2026"));
            Assert.That(ClientReport.PeriodText(null, new DateTime(2026, 10, 31)), Is.EqualTo("hasta 31/10/2026"));
            Assert.That(ClientReport.FileName(new Client { Name = "Andina" }, Printed), Is.EqualTo("Informe de Andina 2026-11-13"));
        }
    }

    /// <summary>"Imprimir" on the list pages and "Informe por cliente…" on Ensayos, against a database.</summary>
    public class ListAndClientReportTests : ServiceFixture
    {
        public ListAndClientReportTests(TestServer server)
            : base(server)
        {
        }

        private static Table DataTable(Document document) => ListAndClientReportLayoutTests.DataTable(document);

        private static string Cell(Table table, int row, int column) => ReportRenderingTests.CellText(table.Rows[row].Cells[column]);

        /// <summary>The next dialog must be the report preview; it is closed at once.</summary>
        private static Func<ReportPreviewViewModel> ExpectPreview(ScriptedDialogs dialogs)
        {
            ReportPreviewViewModel preview = null;
            dialogs.Answer(d =>
            {
                preview = (ReportPreviewViewModel)d;
                return false;
            });
            return () => preview;
        }

        [Test]
        public async Task Clientes_imprimir_prints_everything_the_search_finds_not_only_the_rows_on_screen()
        {
            var tag = Unique("Listado");
            for (var i = 0; i < 105; i++)
            {
                await Services.Clients.SaveAsync(new Client { Name = $"{tag} {i:000}" });
            }

            var archived = (await Services.Clients.SearchAsync(new ListQuery { Text = tag + " 004" })).Items.Single();
            await Services.Clients.SetArchivedAsync(archived, true);

            var dialogs = new ScriptedDialogs();
            var page = new ClientsPageViewModel(Services.Clients, Services.Reports, dialogs, Session) { SearchText = tag };
            await Wait.UntilAsync(() => !page.IsLoading && page.Items.Count == 100);
            Assert.That(page.SupportsPrint, Is.True);
            Assert.That(page.PrintText, Is.EqualTo("Imprimir"));

            var preview = ExpectPreview(dialogs);
            page.PrintCommand.Execute(null);
            Assert.That(preview().Title, Is.EqualTo("Listado de clientes"));
            Assert.That(preview().SuggestedFileName, Is.EqualTo("Listado de clientes 2026-03-10"));
            Assert.That(preview().PageCount, Is.GreaterThan(1), "104 rows don't fit one page");
            Assert.That(preview().Pages[0].Height, Is.GreaterThan(preview().Pages[0].Width), "portrait");

            var active = await Services.Reports.BuildClientListAsync(new ListQuery { Text = tag });
            Assert.That(DataTable(active).Rows.Count - 1, Is.EqualTo(104));

            var all = await Services.Reports.BuildClientListAsync(new ListQuery { Text = tag, IncludeArchived = true });
            Assert.That(DataTable(all).Rows.Count - 1, Is.EqualTo(105));
            Assert.That(Cell(DataTable(all), 5, 1), Is.EqualTo(tag + " 004 (archivado)"), "ordered by name");
            Assert.That(ListAndClientReportLayoutTests.Text(all), Does.Contain("105 clientes · Búsqueda: \"" + tag + "\" · Incluye archivados"));
        }

        [Test]
        public async Task Proveedores_materiales_and_dosificaciones_print_their_lists()
        {
            var design = await NewDesignAsync();
            var material = (await Services.Designs.GetAsync(design.Id)).UsedMaterials.Single();

            var dialogs = new ScriptedDialogs();
            var providers = new ProvidersPageViewModel(Services.Providers, Services.Reports, dialogs, Session) { SearchText = material.ProviderName };
            var preview = ExpectPreview(dialogs);
            providers.PrintCommand.Execute(null);
            Assert.That(preview().Title, Is.EqualTo("Listado de proveedores"));
            var providerList = DataTable(await Services.Reports.BuildProviderListAsync(new ListQuery { Text = material.ProviderName }));
            Assert.That(Cell(providerList, 1, 1), Is.EqualTo(material.ProviderName));

            var materials = new MaterialsPageViewModel(Services.Materials, Services.Providers, Services.Reports, dialogs, Session) { SearchText = material.MaterialName };
            preview = ExpectPreview(dialogs);
            materials.PrintCommand.Execute(null);
            Assert.That(preview().Title, Is.EqualTo("Listado de materiales"));
            var materialList = DataTable(await Services.Reports.BuildMaterialListAsync(new ListQuery { Text = material.MaterialName }));
            Assert.That(materialList.Rows.Count, Is.EqualTo(2));
            Assert.That(Cell(materialList, 1, 3), Is.EqualTo(material.ProviderName));

            var designs = new DesignsPageViewModel(Services.Designs, Services.Materials, Services.Reports, dialogs, Session) { SearchText = design.Description };
            preview = ExpectPreview(dialogs);
            designs.PrintCommand.Execute(null);
            Assert.That(preview().Title, Is.EqualTo("Listado de dosificaciones"));
            var designList = (await Services.Reports.BuildDesignListAsync(new ListQuery { Text = design.Description }))
                .Sections[0].Elements.OfType<Table>().Single();
            Assert.That(designList.Rows.Count, Is.EqualTo(4), "title + heading + 1 material + total");
            Assert.That(Cell(designList, 2, 1), Is.EqualTo(material.MaterialName));
            Assert.That(Cell(designList, 2, 2), Is.EqualTo(material.ProviderName));
            Assert.That(Cell(designList, 2, 4), Is.EqualTo("350"));

            var nothing = await Services.Reports.BuildDesignListAsync(new ListQuery { Text = Unique("no existe") });
            Assert.That(ListAndClientReportLayoutTests.Text(nothing), Does.Contain("Ninguna dosificación"));
        }

        [Test]
        public async Task Informe_por_cliente_prints_the_probetas_of_the_clients_active_ensayos()
        {
            var first = await NewTestAsync();
            var client = await Services.Clients.GetAsync(first.ClientId);
            var second = new CompressionTest
            {
                ClientId = client.Id, ConcreteProviderId = client.Id, ConcreteDesignId = first.ConcreteDesignId,
                ProjectName = "Puente Sur", Address = "Ruta 4", TestDate = Today.AddDays(-10),
            };
            await Services.Tests.SaveAsync(second, testUnlocked: true);
            var archivedTest = new CompressionTest
            {
                ClientId = client.Id, ConcreteProviderId = client.Id, ConcreteDesignId = first.ConcreteDesignId,
                ProjectName = "Archivado", Address = "Ruta 5", TestDate = Today.AddDays(-5),
            };
            await Services.Tests.SaveAsync(archivedTest, testUnlocked: true);
            var other = await NewTestAsync();

            async Task<Specimen> AddAsync(CompressionTest test, decimal? load)
            {
                var specimen = NewSpecimen(test.Id, test.TestDate, 7, load);
                await Services.Specimens.SaveAsync(specimen, testUnlocked: true);
                return specimen;
            }

            var newer = await AddAsync(second, null);
            var older = await AddAsync(first, 40);
            var archivedSpecimen = await AddAsync(first, 41);
            await Services.Specimens.SetArchivedAsync(archivedSpecimen, true);
            await AddAsync(archivedTest, 42);
            await Services.Tests.SetArchivedAsync(archivedTest, true);
            await AddAsync(other, 43);

            var document = await Services.Reports.BuildClientReportAsync(client.Id);
            var table = DataTable(document);
            Assert.That(table.Rows.Count, Is.EqualTo(5), "heading + 2 ensayos + 2 probetas: no archived probetas, archived ensayos or other clients");
            Assert.That(Cell(table, 1, 0), Is.EqualTo($"Ensayo Nº {first.Id} · 08/02/2026 · Edificio Central · fck 210 kg/cm²"), "oldest ensayo first");
            Assert.That(Cell(table, 2, 0), Is.EqualTo(older.Id.ToString()));
            Assert.That(Cell(table, 2, 12), Is.EqualTo("108"), "40 t on Ø 15 cm = 226.35 kg/cm² of 210");
            Assert.That(Cell(table, 3, 0), Is.EqualTo($"Ensayo Nº {second.Id} · 28/02/2026 · Puente Sur · fck 210 kg/cm²"));
            Assert.That(Cell(table, 4, 0), Is.EqualTo(newer.Id.ToString()));

            // Only the ensayos dated within the range (inclusive).
            var recent = await Services.Reports.BuildClientReportAsync(client.Id, second.TestDate, null);
            Assert.That(DataTable(recent).Rows.Count, Is.EqualTo(3), "heading + the newer ensayo + its probeta");
            Assert.That(Cell(DataTable(recent), 2, 0), Is.EqualTo(newer.Id.ToString()));
            Assert.That(Cell(recent.Sections[0].Elements.OfType<Table>().First(), 3, 1), Is.EqualTo("desde 28/02/2026"));
            var early = await Services.Reports.BuildClientReportAsync(client.Id, first.TestDate, first.TestDate);
            Assert.That(Cell(DataTable(early), 2, 0), Is.EqualTo(older.Id.ToString()));
            Assert.That(DataTable(early).Rows.Count, Is.EqualTo(3));
            var none = await Services.Reports.BuildClientReportAsync(client.Id, null, first.TestDate.AddDays(-1));
            Assert.That(ListAndClientReportLayoutTests.Text(none), Does.Contain("El cliente no tiene probetas en ensayos de ese período."));

            // Without a client filter the page asks for the client, then for the dates; with a filter only for the dates.
            void AnswerDates(ScriptedDialogs scripted, string from, string to, bool accept = true) => scripted.Answer(d =>
            {
                var options = (ClientReportOptionsViewModel)d;
                Assert.That(options.ClientName, Is.EqualTo(client.Name));
                if (!accept)
                {
                    return false;
                }

                options.DateFrom = "28/02/2026";
                options.DateTo = "1/2/26";
                options.AcceptCommand.Execute(null);
                Assert.That(options.Errors["DateTo"], Is.EqualTo("La fecha final es anterior a la inicial."));
                options.DateFrom = from;
                options.DateTo = to;
                var closed = false;
                options.CloseRequested += accepted => closed = accepted;
                options.AcceptCommand.Execute(null);
                return closed;
            });

            var dialogs = new ScriptedDialogs();
            var page = new TestsPageViewModel(Services, dialogs, new RecordingNavigator());
            Assert.That(page.PrintText, Is.EqualTo("Informe por cliente…"));
            dialogs.Answer(d =>
            {
                var picker = (ClientPickerViewModel)d;
                Assert.That(picker.Title, Is.EqualTo("Informe por cliente"));
                picker.SearchDelayMs = 0;
                picker.SearchText = client.Name;
                Wait.UntilAsync(() => !picker.IsLoading && picker.SelectedItem?.Id == client.Id).GetAwaiter().GetResult();
                return true;
            });
            AnswerDates(dialogs, "28/02/2026", string.Empty);
            var preview = ExpectPreview(dialogs);
            page.PrintCommand.Execute(null);
            Assert.That(preview().Title, Is.EqualTo("Informe de " + client.Name));
            Assert.That(preview().SuggestedFileName, Is.EqualTo("Informe de " + client.Name + " 2026-03-10"));
            Assert.That(preview().Pages.Single().Width, Is.GreaterThan(preview().Pages.Single().Height), "landscape");
            var options = dialogs.Shown.OfType<ClientReportOptionsViewModel>().Single();
            Assert.That(options.From, Is.EqualTo(second.TestDate));
            Assert.That(options.To, Is.Null, "empty = open");

            page.ClientFilter = client;
            Assert.That(page.PrintToolTip, Does.Contain(client.Name));
            AnswerDates(dialogs, string.Empty, string.Empty);
            preview = ExpectPreview(dialogs);
            page.PrintCommand.Execute(null);
            Assert.That(preview().Title, Is.EqualTo("Informe de " + client.Name));

            // Cancelling the dates or the client search opens nothing.
            AnswerDates(dialogs, null, null, accept: false);
            page.PrintCommand.Execute(null);
            Assert.That(dialogs.Shown.Last(), Is.InstanceOf<ClientReportOptionsViewModel>());
            page.ClientFilter = null;
            dialogs.Answer(_ => false);
            page.PrintCommand.Execute(null);
            Assert.That(dialogs.Shown.Last(), Is.InstanceOf<ClientPickerViewModel>());
        }

        [Test]
        public async Task Client_without_probetas_gets_a_report_that_says_so()
        {
            var client = await NewClientAsync();
            var document = await Services.Reports.BuildClientReportAsync(client.Id);

            Assert.That(ListAndClientReportLayoutTests.Text(document), Does.Contain("El cliente no tiene probetas en sus ensayos."));
            Assert.That(new ReportRenderer(document).PageCount, Is.EqualTo(1));
            Assert.ThrowsAsync<ProbetaControl.Services.NotFoundException>(() => Services.Reports.BuildClientReportAsync(int.MaxValue));
        }
    }
}
