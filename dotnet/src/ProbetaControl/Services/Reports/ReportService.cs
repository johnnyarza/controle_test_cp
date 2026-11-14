using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MigraDoc.DocumentObjectModel;
using ProbetaControl.Models;

namespace ProbetaControl.Services.Reports
{
    /// <summary>Reads what a report needs (always fresh from the database) and lays it out off the UI thread.</summary>
    public sealed class ReportService
    {
        /// <summary>
        /// Most rows a list report or the report by client prints (about 100 pages, some 15 s to lay out).
        /// Beyond it the report says it was cut short, instead of keeping an old PC busy for minutes.
        /// </summary>
        public const int MaxRows = 3000;

        private readonly CompressionTestService _tests;
        private readonly ConcreteDesignService _designs;
        private readonly SpecimenService _specimens;
        private readonly ClientService _clients;
        private readonly ProviderService _providers;
        private readonly MaterialService _materials;
        private readonly ReportSettingsService _settings;
        private readonly IClock _clock;

        public ReportService(CompressionTestService tests, ConcreteDesignService designs, SpecimenService specimens,
            ClientService clients, ProviderService providers, MaterialService materials,
            ReportSettingsService settings, IClock clock)
        {
            _tests = tests;
            _designs = designs;
            _specimens = specimens;
            _clients = clients;
            _providers = providers;
            _materials = materials;
            _settings = settings;
            _clock = clock;
        }

        /// <summary>The date printed on reports and used in their file names.</summary>
        public DateTime Today => _clock.Today;

        /// <summary>
        /// The ensayo report with the probetas that match <paramref name="filter"/> (null = all);
        /// archived probetas are never printed.
        /// </summary>
        public async Task<Document> BuildTestReportAsync(int testId, SpecimenFilter filter, CancellationToken cancellationToken = default)
        {
            var data = await GetTestReportDataAsync(testId, filter, cancellationToken);
            return await Task.Run(() => TestReport.Build(data), cancellationToken);
        }

        /// <summary>
        /// The ensayo as an Excel workbook at <paramref name="path"/> (replaced if it exists), with the same probetas
        /// the report would print. The file being open in Excel throws <see cref="System.IO.IOException"/>.
        /// </summary>
        public async Task ExportTestToExcelAsync(int testId, SpecimenFilter filter, string path, CancellationToken cancellationToken = default)
        {
            var data = await GetTestReportDataAsync(testId, filter, cancellationToken);
            await Task.Run(() => TestExcelExport.Save(data, path), cancellationToken);
        }

        /// <summary>The ensayo, its design and the probetas matching the filter (never archived ones), read fresh.</summary>
        private async Task<TestReportData> GetTestReportDataAsync(int testId, SpecimenFilter filter, CancellationToken cancellationToken)
        {
            var test = await _tests.GetAsync(testId, cancellationToken) ?? throw new NotFoundException("el ensayo");
            var printFilter = new SpecimenFilter
            {
                MoldDateFrom = filter?.MoldDateFrom,
                MoldDateTo = filter?.MoldDateTo,
                IdRanges = filter?.IdRanges ?? new SpecimenFilter().IdRanges,
                IncludeArchived = false,
            };
            var specimens = await _specimens.ListByTestAsync(testId, printFilter, cancellationToken);
            var design = await _designs.GetAsync(test.ConcreteDesignId, cancellationToken);
            var branding = await _settings.GetBrandingAsync(cancellationToken);

            return new TestReportData
            {
                Test = test,
                Design = design,
                Specimens = specimens.Where(s => !s.IsArchived).OrderBy(s => s.Id).ToList(),
                Branding = branding,
                PrintedOn = _clock.Today,
            };
        }

        /// <summary>
        /// The report by client: every active probeta of the client's active ensayos (as their client, not as
        /// concrete provider), oldest ensayo first. The dates (inclusive, null = open) keep only the ensayos dated
        /// within them.
        /// </summary>
        public async Task<Document> BuildClientReportAsync(int clientId, DateTime? testDateFrom = null, DateTime? testDateTo = null,
            CancellationToken cancellationToken = default)
        {
            var client = await _clients.GetAsync(clientId, cancellationToken) ?? throw new NotFoundException("el cliente");
            var specimens = await _specimens.ListByClientAsync(clientId, testDateFrom, testDateTo, MaxRows + 1, cancellationToken);
            var branding = await _settings.GetBrandingAsync(cancellationToken);

            var data = new ClientReportData
            {
                Client = client,
                TestDateFrom = testDateFrom?.Date,
                TestDateTo = testDateTo?.Date,
                Specimens = specimens.Take(MaxRows).ToList(),
                IsCutShort = specimens.Count > MaxRows,
                Branding = branding,
                PrintedOn = _clock.Today,
            };
            return await Task.Run(() => ClientReport.Build(data), cancellationToken);
        }

        /// <summary>
        /// "Listado de clientes" with every client the list page's search finds (<paramref name="query"/>'s text
        /// and "Mostrar archivados"; its page is ignored).
        /// </summary>
        public Task<Document> BuildClientListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
            BuildListAsync(query, _clients.SearchAsync, ListReports.BuildClients, cancellationToken);

        public Task<Document> BuildProviderListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
            BuildListAsync(query, _providers.SearchAsync, ListReports.BuildProviders, cancellationToken);

        public Task<Document> BuildMaterialListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
            BuildListAsync(query, _materials.SearchAsync, ListReports.BuildMaterials, cancellationToken);

        public Task<Document> BuildDesignListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
            BuildListAsync(query, _designs.SearchAsync, ListReports.BuildDesigns, cancellationToken);

        /// <summary>"2026-10-01 Ensayo 12 - Constructora Andina - Torre Norte" (legacy Excel name order).</summary>
        public static string TestFileName(CompressionTest test) => string.Format(CultureInfo.InvariantCulture,
            "{0:yyyy-MM-dd} Ensayo {1} - {2} - {3}", test.TestDate, test.Id, test.ClientName, test.ProjectName);

        /// <summary>Reads the whole search result page by page (up to <see cref="MaxRows"/>) and lays it out.</summary>
        private async Task<Document> BuildListAsync<T>(ListQuery query,
            Func<ListQuery, CancellationToken, Task<PagedResult<T>>> search,
            Func<ListReportData<T>, Document> build, CancellationToken cancellationToken)
        {
            var items = new List<T>();
            var total = 0;
            while (items.Count < MaxRows)
            {
                var page = await search(new ListQuery
                {
                    Text = query?.Text,
                    IncludeArchived = query?.IncludeArchived ?? false,
                    Page = new PageRequest(items.Count, Math.Min(PageRequest.MaxSize, MaxRows - items.Count)),
                }, cancellationToken);
                total = page.TotalCount;
                items.AddRange(page.Items);
                if (page.Items.Count == 0 || items.Count >= total)
                {
                    break;
                }
            }

            var branding = await _settings.GetBrandingAsync(cancellationToken);
            var data = new ListReportData<T>
            {
                Items = items,
                TotalCount = total,
                SearchText = query?.Text,
                IncludeArchived = query?.IncludeArchived ?? false,
                Branding = branding,
                PrintedOn = _clock.Today,
            };
            return await Task.Run(() => build(data), cancellationToken);
        }
    }
}
