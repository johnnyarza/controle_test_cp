using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Services.Reports;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Settings
{
    /// <summary>
    /// Etiquetas (administrators): the server name in the QR links and the label sheet (a common A4 sheet or
    /// custom measurements). "Hoja de prueba" prints the outlines of the labels on plain paper, to check the
    /// measurements against a real sheet before saving them. Stored in the database, the same on every PC.
    /// </summary>
    public sealed class LabelsSectionViewModel : SettingsSection
    {
        public static readonly LabelSheetPreset Custom = new LabelSheetPreset("Personalizada", null);

        private const string ExampleCode = "3F9A0C17B2D4E856";

        private readonly LabelSettingsService _service;
        private readonly ReportService _reports;
        private readonly IDialogService _dialogs;
        private string _serverName;
        private string _columns;
        private string _rows;
        private string _widthMm;
        private string _heightMm;
        private string _topMm;
        private string _leftMm;
        private string _gapXMm;
        private string _gapYMm;
        private LabelSheetPreset _selectedPreset;
        private bool _filling;

        public LabelsSectionViewModel(LabelSettingsService service, ReportService reports, IDialogService dialogs)
            : base(SettingsPageViewModel.AdminGroup, "Etiquetas", "Icon.Qr")
        {
            _service = service;
            _reports = reports;
            _dialogs = dialogs;
            Presets = LabelSheet.Presets.Concat(new[] { Custom }).ToList();
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
            TestSheetCommand = new RelayCommand(PrintTestSheet, () => !IsBusy);
        }

        /// <summary>The lab server's name on the network, e.g. "servidor-lab".</summary>
        public string ServerName
        {
            get => _serverName;
            set
            {
                if (SetProperty(ref _serverName, value))
                {
                    OnPropertyChanged(nameof(LinkExample));
                }
            }
        }

        public int MaxServerNameLength => LabelLink.MaxServerNameLength;

        /// <summary>"Ejemplo: http://servidor-lab/p/3F9A0C17B2D4E856".</summary>
        public string LinkExample
        {
            get
            {
                var server = LabelLink.Normalize(ServerName);
                return "Cada QR lleva una dirección como http://" + (server ?? "nombre-del-servidor") + "/p/" + ExampleCode + ".";
            }
        }

        /// <summary>The common sheets and "Personalizada" (shown when the measurements match none of them).</summary>
        public IReadOnlyList<LabelSheetPreset> Presets { get; }

        public LabelSheetPreset SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (value == null || !SetProperty(ref _selectedPreset, value) || _filling || value.Sheet == null)
                {
                    return;
                }

                Fill(value.Sheet);
            }
        }

        public string Columns { get => _columns; set => SetSheetField(ref _columns, value); }

        public string Rows { get => _rows; set => SetSheetField(ref _rows, value); }

        public string WidthMm { get => _widthMm; set => SetSheetField(ref _widthMm, value); }

        public string HeightMm { get => _heightMm; set => SetSheetField(ref _heightMm, value); }

        public string TopMm { get => _topMm; set => SetSheetField(ref _topMm, value); }

        public string LeftMm { get => _leftMm; set => SetSheetField(ref _leftMm, value); }

        public string GapXMm { get => _gapXMm; set => SetSheetField(ref _gapXMm, value); }

        public string GapYMm { get => _gapYMm; set => SetSheetField(ref _gapYMm, value); }

        /// <summary>"24 etiquetas por hoja" while the measurements make sense.</summary>
        public string SheetSummary
        {
            get
            {
                var sheet = ReadSheet(new ValidationErrors());
                return sheet == null || sheet.Validate().HasErrors
                    ? null
                    : sheet.PerSheet.ToString(CultureInfo.InvariantCulture) + " etiquetas por hoja";
            }
        }

        public ICommand SaveCommand { get; }

        /// <summary>The label outlines with the measurements as typed (not saved yet), in the preview.</summary>
        public ICommand TestSheetCommand { get; }

        public override Task LoadAsync() => RunAsync(async () =>
        {
            var settings = await _service.GetAsync();
            ServerName = settings.ServerName;
            Fill(settings.Sheet);
        });

        private async Task SaveAsync()
        {
            var errors = new ValidationErrors();
            var sheet = ReadSheet(errors);
            if (errors.HasErrors)
            {
                ClearMessage();
                Errors.Set(errors.ToDictionary());
                return;
            }

            var ok = await RunAsync(() => _service.SaveAsync(new LabelSettings { ServerName = ServerName, Sheet = sheet }));
            if (ok)
            {
                ServerName = LabelLink.Normalize(ServerName);
                ShowMessage(MessageKind.Success, "Datos guardados. Las próximas etiquetas saldrán así en todos los equipos.");
            }
        }

        private void PrintTestSheet()
        {
            ClearMessage();
            Errors.Clear();
            var errors = new ValidationErrors();
            var sheet = ReadSheet(errors);
            if (sheet != null)
            {
                sheet.Validate(errors);
            }

            if (errors.HasErrors)
            {
                Errors.Set(errors.ToDictionary());
                return;
            }

            _dialogs.Show(new Reports.ReportPreviewViewModel("Hoja de prueba de etiquetas", "Hoja de prueba de etiquetas",
                ct => _reports.BuildLabelTestSheetAsync(sheet, ct), _dialogs));
        }

        /// <summary>The sheet as typed; null with errors (by field) when a number can't be read.</summary>
        private LabelSheet ReadSheet(ValidationErrors errors)
        {
            int Whole(string field, string text)
            {
                var value = NumberInput.Parse(field, text, errors, required: true);
                if (value.HasValue && value.Value != decimal.Truncate(value.Value))
                {
                    errors.Add(field, "Ingrese un número entero.");
                }

                return value.HasValue && value.Value >= 0 && value.Value <= 1000 ? (int)value.Value : 0;
            }

            decimal Mm(string field, string text) => NumberInput.Parse(field, text, errors, required: true) ?? 0;

            var sheet = new LabelSheet
            {
                Columns = Whole(nameof(Columns), Columns),
                Rows = Whole(nameof(Rows), Rows),
                WidthMm = Mm(nameof(WidthMm), WidthMm),
                HeightMm = Mm(nameof(HeightMm), HeightMm),
                TopMm = Mm(nameof(TopMm), TopMm),
                LeftMm = Mm(nameof(LeftMm), LeftMm),
                GapXMm = Mm(nameof(GapXMm), GapXMm),
                GapYMm = Mm(nameof(GapYMm), GapYMm),
            };
            return errors.HasErrors ? null : sheet;
        }

        private void Fill(LabelSheet sheet)
        {
            _filling = true;
            try
            {
                Columns = sheet.Columns.ToString(CultureInfo.InvariantCulture);
                Rows = sheet.Rows.ToString(CultureInfo.InvariantCulture);
                WidthMm = NumberInput.Format(sheet.WidthMm);
                HeightMm = NumberInput.Format(sheet.HeightMm);
                TopMm = NumberInput.Format(sheet.TopMm);
                LeftMm = NumberInput.Format(sheet.LeftMm);
                GapXMm = NumberInput.Format(sheet.GapXMm);
                GapYMm = NumberInput.Format(sheet.GapYMm);
            }
            finally
            {
                _filling = false;
            }

            MatchPreset();
        }

        private void SetSheetField(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (!SetProperty(ref field, value, name))
            {
                return;
            }

            OnPropertyChanged(nameof(SheetSummary));
            if (!_filling)
            {
                MatchPreset();
            }
        }

        /// <summary>Shows the preset the measurements match, or "Personalizada".</summary>
        private void MatchPreset()
        {
            var sheet = ReadSheet(new ValidationErrors());
            var match = sheet == null ? null : LabelSheet.Presets.FirstOrDefault(p => p.Sheet.SameAs(sheet));
            _filling = true;
            try
            {
                SelectedPreset = match ?? Custom;
            }
            finally
            {
                _filling = false;
            }
        }
    }
}
