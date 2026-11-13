using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProbetaControl.Data;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    /// <summary>The lab details printed on reports. Everyone reads them; only administrators change them.</summary>
    public sealed class ReportSettingsService
    {
        private static readonly string[] Keys =
        {
            AppSettingsRepository.ReportLabName,
            AppSettingsRepository.ReportFooter,
            AppSettingsRepository.ReportNote,
            AppSettingsRepository.ReportPaperSize,
        };

        private readonly AppSettingsRepository _repository;
        private readonly ReportImageService _images;
        private readonly AppSession _session;

        public ReportSettingsService(AppSettingsRepository repository, ReportImageService images, AppSession session)
        {
            _repository = repository;
            _images = images;
            _session = session;
        }

        public async Task<ReportSettings> GetAsync(CancellationToken cancellationToken = default)
        {
            var values = await _repository.GetTextsAsync(Keys, cancellationToken).ConfigureAwait(false);
            string Value(string key) => values.TryGetValue(key, out var value) ? value : null;
            return new ReportSettings
            {
                LabName = Value(AppSettingsRepository.ReportLabName),
                FooterText = Value(AppSettingsRepository.ReportFooter),
                NoteText = Value(AppSettingsRepository.ReportNote),
                PaperSize = Value(AppSettingsRepository.ReportPaperSize) == "Letter" ? ReportPaperSize.Letter : ReportPaperSize.A4,
            };
        }

        /// <summary>Lab details plus logo and stamp, read fresh for each report.</summary>
        public async Task<ReportBranding> GetBrandingAsync(CancellationToken cancellationToken = default)
        {
            var settings = await GetAsync(cancellationToken).ConfigureAwait(false);
            var logo = await _images.GetLogoAsync(cancellationToken).ConfigureAwait(false);
            var stamp = await _images.GetStampAsync(cancellationToken).ConfigureAwait(false);
            return new ReportBranding(settings, logo?.Bytes, stamp?.Bytes);
        }

        public Task SaveAsync(ReportSettings settings, CancellationToken cancellationToken = default)
        {
            _session.RequireAdmin();
            new ValidationErrors()
                .Required(nameof(ReportSettings.LabName), settings.LabName)
                .MaxLength(nameof(ReportSettings.LabName), settings.LabName, ReportSettings.MaxLabNameLength)
                .MaxLength(nameof(ReportSettings.FooterText), settings.FooterText, ReportSettings.MaxTextLength)
                .MaxLength(nameof(ReportSettings.NoteText), settings.NoteText, ReportSettings.MaxTextLength)
                .ThrowIfAny();

            return _repository.SetTextsAsync(new Dictionary<string, string>
            {
                [AppSettingsRepository.ReportLabName] = Clean(settings.LabName),
                [AppSettingsRepository.ReportFooter] = Clean(settings.FooterText),
                [AppSettingsRepository.ReportNote] = Clean(settings.NoteText),
                [AppSettingsRepository.ReportPaperSize] = settings.PaperSize == ReportPaperSize.Letter ? "Letter" : "A4",
            }, cancellationToken);
        }

        private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
