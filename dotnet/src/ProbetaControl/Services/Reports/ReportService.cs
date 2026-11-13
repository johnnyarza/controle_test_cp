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
        private readonly CompressionTestService _tests;
        private readonly ConcreteDesignService _designs;
        private readonly SpecimenService _specimens;
        private readonly ReportSettingsService _settings;
        private readonly IClock _clock;

        public ReportService(CompressionTestService tests, ConcreteDesignService designs, SpecimenService specimens,
            ReportSettingsService settings, IClock clock)
        {
            _tests = tests;
            _designs = designs;
            _specimens = specimens;
            _settings = settings;
            _clock = clock;
        }

        /// <summary>
        /// The ensayo report with the probetas that match <paramref name="filter"/> (null = all);
        /// archived probetas are never printed.
        /// </summary>
        public async Task<Document> BuildTestReportAsync(int testId, SpecimenFilter filter, CancellationToken cancellationToken = default)
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

            var data = new TestReportData
            {
                Test = test,
                Design = design,
                Specimens = specimens.Where(s => !s.IsArchived).OrderBy(s => s.Id).ToList(),
                Branding = branding,
                PrintedOn = _clock.Today,
            };
            return await Task.Run(() => TestReport.Build(data), cancellationToken);
        }

        /// <summary>"2026-10-01 Ensayo 12 - Constructora Andina - Torre Norte" (legacy Excel name order).</summary>
        public static string TestFileName(CompressionTest test) => string.Format(CultureInfo.InvariantCulture,
            "{0:yyyy-MM-dd} Ensayo {1} - {2} - {3}", test.TestDate, test.Id, test.ClientName, test.ProjectName);
    }
}
