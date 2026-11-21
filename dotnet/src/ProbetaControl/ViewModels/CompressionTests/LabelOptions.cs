using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.CompressionTests
{
    /// <summary>
    /// "Etiquetas QR": which probetas get a label and where the first one goes on the sheet. A small picture of the
    /// sheet shows the labels already used (before the chosen start), the ones this print fills and the free ones;
    /// clicking a label starts there, so a sheet used before can be finished. The settings are read fresh; without
    /// a server name for the link nothing can be printed.
    /// </summary>
    public sealed class LabelOptionsViewModel : DialogViewModel
    {
        private readonly LabelSettingsService _settings;
        private readonly IReadOnlyList<Specimen> _specimens;
        private LabelSheet _sheet;
        private string _serverName;
        private bool _onlyUntested;
        private int _startPosition = 1;

        /// <param name="specimens">Candidates (the page's active probetas, or the one chosen).</param>
        /// <param name="scopeText">Under the title, e.g. "Ensayo Nº 12 · probetas del filtro".</param>
        /// <param name="single">One chosen probeta: printed even if it is already broken (a label to replace).</param>
        /// <param name="brokenChosen">
        /// The page's filter asks for broken probetas (Estado = Rotas): that choice wins, so "Solo probetas sin romper"
        /// is hidden and off instead of leaving nothing to print.
        /// </param>
        public LabelOptionsViewModel(LabelSettingsService settings, IReadOnlyList<Specimen> specimens, string scopeText, bool single,
            bool brokenChosen = false)
        {
            _settings = settings;
            _specimens = specimens.Where(s => !s.IsArchived).ToList();
            ScopeText = scopeText;
            IsSingle = single;
            BrokenChosen = brokenChosen;
            _onlyUntested = !single && !brokenChosen;
            AcceptCommand = new RelayCommand(Accept, () => CanPrint);
            CancelCommand = new RelayCommand(() => Close(false));
            ChooseStartCommand = new RelayCommand(
                p => StartPosition = ((LabelPositionViewModel)p).Number, p => p is LabelPositionViewModel);
        }

        public override string Title => "Etiquetas QR";

        public override double PreferredWidth => 540;

        public string ScopeText { get; }

        public bool IsSingle { get; }

        /// <summary>Estado = Rotas in the page's filter (see the constructor).</summary>
        public bool BrokenChosen { get; }

        /// <summary>The checkbox only matters when some candidates are broken already and nobody asked for broken ones.</summary>
        public bool ShowOnlyUntested => !IsSingle && !BrokenChosen && _specimens.Any(s => s.IsTested);

        /// <summary>"Solo probetas sin romper" (default): labels are stuck on before the probetas are broken.</summary>
        public bool OnlyUntested
        {
            get => _onlyUntested;
            set
            {
                if (SetProperty(ref _onlyUntested, value))
                {
                    Refresh();
                }
            }
        }

        public int LabelCount => _specimens.Count(s => !(OnlyUntested && s.IsTested));

        /// <summary>1 = top left of the first sheet, numbered along each row.</summary>
        public int StartPosition
        {
            get => _startPosition;
            set
            {
                var max = _sheet?.PerSheet ?? 1;
                value = value < 1 ? 1 : value > max ? max : value;
                if (SetProperty(ref _startPosition, value))
                {
                    Refresh();
                }
            }
        }

        /// <summary>The first sheet, label by label (bound to a grid with <see cref="Columns"/> columns).</summary>
        public ObservableCollection<LabelPositionViewModel> Positions { get; } = new ObservableCollection<LabelPositionViewModel>();

        public int Columns => _sheet?.Columns ?? 1;

        /// <summary>Width of the sheet picture; its height follows the paper (A4).</summary>
        public double SheetPictureWidth => 150;

        public double SheetPictureHeight => SheetPictureWidth * (double)(LabelSheet.PaperHeightMm / LabelSheet.PaperWidthMm);

        /// <summary>"Hoja A4 · 3 × 8 etiquetas de 70 × 37 mm".</summary>
        public string SheetText => _sheet == null ? null : "Hoja A4 · " + _sheet.Description;

        /// <summary>"7 etiquetas en 1 hoja, de la 5 a la 11." / "No hay probetas sin romper."</summary>
        public string SummaryText
        {
            get
            {
                if (_sheet == null)
                {
                    return null;
                }

                var count = LabelCount;
                if (count == 0)
                {
                    return OnlyUntested && _specimens.Count > 0 ? "Todas las probetas ya están rotas." : "No hay probetas para etiquetar.";
                }

                var sheets = Services.Reports.SpecimenLabels.SheetCount(count, StartPosition, _sheet.PerSheet);
                var labels = count == 1 ? "1 etiqueta" : AppCulture.Count(count) + " etiquetas";
                var pages = sheets == 1 ? "1 hoja" : AppCulture.Count(sheets) + " hojas";
                var last = StartPosition + count - 1;
                return sheets == 1
                    ? count == 1
                        ? $"{labels}, en la posición {StartPosition}."
                        : $"{labels} en {pages}, de la {StartPosition} a la {last}."
                    : $"{labels} en {pages}, empezando en la etiqueta {StartPosition} de la primera.";
            }
        }

        public bool CanPrint => _sheet != null && _serverName != null && LabelCount > 0 && !IsBusy;

        public ICommand AcceptCommand { get; }

        public ICommand CancelCommand { get; }

        /// <summary>A label of the sheet picture: the print starts there.</summary>
        public ICommand ChooseStartCommand { get; }

        public override async Task InitializeAsync()
        {
            LabelSettings settings = null;
            await RunAsync(async () => settings = await _settings.GetAsync());
            if (settings == null)
            {
                return;
            }

            _sheet = settings.Sheet;
            _serverName = settings.ServerName;
            if (_serverName == null)
            {
                ShowMessage(MessageKind.Error, Services.Reports.ReportService.NoServerNameMessage);
            }

            OnPropertyChanged(nameof(Columns));
            OnPropertyChanged(nameof(SheetText));
            Refresh();
        }

        private void Refresh()
        {
            OnPropertyChanged(nameof(LabelCount));
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(CanPrint));
            if (_sheet == null)
            {
                return;
            }

            var printed = LabelCount;
            var positions = Enumerable.Range(1, _sheet.PerSheet).Select(n => new LabelPositionViewModel(n,
                n < StartPosition ? LabelPositionState.Used
                : n < StartPosition + printed ? LabelPositionState.Printed
                : LabelPositionState.Free,
                n == StartPosition)).ToList();
            Positions.Clear();
            foreach (var position in positions)
            {
                Positions.Add(position);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void Accept()
        {
            if (CanPrint)
            {
                Close(true);
            }
        }
    }

    public enum LabelPositionState
    {
        /// <summary>Before the start: already used on this sheet.</summary>
        Used,

        /// <summary>Gets a label in this print.</summary>
        Printed,

        /// <summary>Left free on the first sheet.</summary>
        Free,
    }

    public sealed class LabelPositionViewModel
    {
        public LabelPositionViewModel(int number, LabelPositionState state, bool isStart)
        {
            Number = number;
            State = state;
            IsStart = isStart;
        }

        public int Number { get; }

        public LabelPositionState State { get; }

        public bool IsStart { get; }

        /// <summary>Accessible name and tooltip: "Etiqueta 5: se imprime".</summary>
        public string Description => "Etiqueta " + Number.ToString(CultureInfo.InvariantCulture) + ": " +
            (State == LabelPositionState.Used ? "ya usada" : State == LabelPositionState.Printed ? "se imprime" : "queda libre") +
            ". Clic para empezar aquí.";

        public override string ToString() => Description;
    }
}
