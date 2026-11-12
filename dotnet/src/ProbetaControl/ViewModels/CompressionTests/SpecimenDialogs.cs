using System;
using System.Collections.Generic;
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
    /// New, copied or existing probeta. Fecha de moldeo + Edad (días) give the rupture date (and the other way
    /// round); "Hoy" fills the mold date; "Promedio" averages two readings of the diameter or height. Density and
    /// strength are previewed as you type. What may change follows <see cref="SpecimenEditPolicy"/>.
    /// </summary>
    public sealed class SpecimenEditorViewModel : EditorDialogViewModel
    {
        public const string RuptureLoadOnlyNotice =
            "Ensayo bloqueado: solo puede registrar la rotura. Para cambiar las medidas, un administrador debe desbloquear el ensayo.";

        private const int MaxAgeDays = 3650;

        private readonly AppServices _services;
        private readonly IDialogService _dialogs;
        private readonly int _testId;
        private readonly int? _id;
        private readonly Specimen _copyFrom;
        private Specimen _record = new Specimen();
        private bool _syncing;
        private string _code = string.Empty;
        private string _slump = string.Empty;
        private string _moldDate = string.Empty;
        private string _ageDays = string.Empty;
        private string _ruptureDate = string.Empty;
        private string _diameterCm = string.Empty;
        private string _heightCm = string.Empty;
        private string _weightKg = string.Empty;
        private string _ruptureLoadTon = string.Empty;
        private string _notice;
        private bool _isMeasurementsReadOnly;
        private bool _isLoadReadOnly;

        private readonly decimal _designFck;
        private readonly Specimen _startFrom;

        private SpecimenEditorViewModel(AppServices services, IDialogService dialogs, int testId, int? id, Specimen copyFrom,
            decimal designFck, Specimen startFrom = null)
        {
            _services = services;
            _dialogs = dialogs;
            _testId = testId;
            _id = id;
            _copyFrom = copyFrom;
            _designFck = designFck;
            _startFrom = startFrom;

            TodayCommand = new RelayCommand(() => MoldDate = DateInput.Format(_services.Specimens.Today), () => CanEditMeasurements);
            AgeCommand = new RelayCommand(
                (object days) => AgeDays = Convert.ToString(days, CultureInfo.InvariantCulture), (object _) => CanEditMeasurements);
            AverageDiameterCommand = new RelayCommand(() => Average("Promedio del diámetro", "Diámetro (cm)", 2, v => DiameterCm = v),
                () => CanEditMeasurements);
            AverageHeightCommand = new RelayCommand(() => Average("Promedio de la altura", "Altura (cm)", 2, v => HeightCm = v),
                () => CanEditMeasurements);
        }

        /// <param name="designFck">Design strength, for "% fck" next to the strength (0 = not shown).</param>
        /// <param name="last">The ensayo's newest probeta: its slump, diameter and height are filled in (one pour usually shares them).</param>
        public static SpecimenEditorViewModel New(AppServices services, IDialogService dialogs, int testId, decimal designFck = 0,
            Specimen last = null) =>
            new SpecimenEditorViewModel(services, dialogs, testId, null, null, designFck, last);

        public static SpecimenEditorViewModel Edit(AppServices services, IDialogService dialogs, int testId, int specimenId,
            decimal designFck = 0) =>
            new SpecimenEditorViewModel(services, dialogs, testId, specimenId, null, designFck);

        /// <summary>A new probeta with the measurements of <paramref name="source"/> (not its rupture load).</summary>
        public static SpecimenEditorViewModel Copy(AppServices services, IDialogService dialogs, int testId, Specimen source,
            decimal designFck = 0) =>
            new SpecimenEditorViewModel(services, dialogs, testId, null, source, designFck);

        /// <summary>"Slump, diámetro y altura de la probeta Nº 12." on a new probeta started from the last one.</summary>
        public string StartNote { get; private set; }

        public override string Title => _id != null
            ? $"Probeta Nº {_id}"
            : _copyFrom != null ? $"Copiar probeta Nº {_copyFrom.Id}" : "Nueva probeta";

        public override double PreferredWidth => 700;

        /// <summary>Descripción.</summary>
        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        /// <summary>cm.</summary>
        public string Slump
        {
            get => _slump;
            set => SetProperty(ref _slump, value);
        }

        public string MoldDate
        {
            get => _moldDate;
            set
            {
                if (SetProperty(ref _moldDate, value) && !_syncing)
                {
                    // Keep the age: the rupture date moves with the mold date.
                    if (TryAge(out _))
                    {
                        UpdateRuptureFromAge();
                    }
                    else
                    {
                        UpdateAgeFromDates();
                    }
                }
            }
        }

        /// <summary>Days from molding to rupture; typing it sets the rupture date.</summary>
        public string AgeDays
        {
            get => _ageDays;
            set
            {
                if (SetProperty(ref _ageDays, value) && !_syncing)
                {
                    UpdateRuptureFromAge();
                }
            }
        }

        public string RuptureDate
        {
            get => _ruptureDate;
            set
            {
                if (SetProperty(ref _ruptureDate, value) && !_syncing)
                {
                    UpdateAgeFromDates();
                }
            }
        }

        public string DiameterCm
        {
            get => _diameterCm;
            set
            {
                if (SetProperty(ref _diameterCm, value))
                {
                    RaisePreview();
                }
            }
        }

        public string HeightCm
        {
            get => _heightCm;
            set
            {
                if (SetProperty(ref _heightCm, value))
                {
                    RaisePreview();
                }
            }
        }

        public string WeightKg
        {
            get => _weightKg;
            set
            {
                if (SetProperty(ref _weightKg, value))
                {
                    RaisePreview();
                }
            }
        }

        /// <summary>Rotura (t); empty until the probeta is broken.</summary>
        public string RuptureLoadTon
        {
            get => _ruptureLoadTon;
            set
            {
                if (SetProperty(ref _ruptureLoadTon, value))
                {
                    RaisePreview();
                }
            }
        }

        /// <summary>kg/m³ from the typed diameter, height and weight.</summary>
        public string DensityText => FormatPreview(SpecimenMath.DensityKgPerM3(Number(DiameterCm), Number(HeightCm), Number(WeightKg)), "kg/m³");

        /// <summary>kg/cm² from the typed diameter and rupture load.</summary>
        public string StrengthText
        {
            get
            {
                NumberInput.TryParse(RuptureLoadTon, out var load);
                var strength = SpecimenMath.StrengthKgPerCm2(Number(DiameterCm), load);
                var percent = SpecimenMath.PercentOfFck(strength, _designFck);
                return FormatPreview(strength, "kg/cm²")
                       + (percent.HasValue ? string.Format(CultureInfo.InvariantCulture, " ({0:0} % fck)", percent.Value) : string.Empty);
            }
        }

        /// <summary>Strength only, for the result tile ("226.35 kg/cm²" or "—").</summary>
        public string StrengthValueText
        {
            get
            {
                NumberInput.TryParse(RuptureLoadTon, out var load);
                return FormatPreview(SpecimenMath.StrengthKgPerCm2(Number(DiameterCm), load), "kg/cm²");
            }
        }

        /// <summary>"108 %" of the design fck, or "—".</summary>
        public string PercentText
        {
            get
            {
                NumberInput.TryParse(RuptureLoadTon, out var load);
                var percent = SpecimenMath.PercentOfFck(SpecimenMath.StrengthKgPerCm2(Number(DiameterCm), load), _designFck);
                return percent.HasValue ? string.Format(CultureInfo.InvariantCulture, "{0:0} %", percent.Value) : "—";
            }
        }

        /// <summary>"% del fck (210)".</summary>
        public string PercentLabel => _designFck > 0
            ? string.Format(CultureInfo.InvariantCulture, "% del fck ({0:0.##})", _designFck)
            : "% del fck";

        /// <summary>Why fields are read-only (locked test, probeta already broken…); null when all can change.</summary>
        public string Notice
        {
            get => _notice;
            private set => SetProperty(ref _notice, value);
        }

        public bool IsMeasurementsReadOnly
        {
            get => _isMeasurementsReadOnly;
            private set
            {
                if (SetProperty(ref _isMeasurementsReadOnly, value))
                {
                    RaiseEditState();
                }
            }
        }

        public bool IsLoadReadOnly
        {
            get => _isLoadReadOnly;
            private set
            {
                if (SetProperty(ref _isLoadReadOnly, value))
                {
                    RaiseEditState();
                }
            }
        }

        public bool CanEditMeasurements => !IsMeasurementsReadOnly && !IsBusy;

        public bool CanSave => !(IsMeasurementsReadOnly && IsLoadReadOnly);

        public string CloseText => CanSave ? "Cancelar" : "Cerrar";

        public ICommand TodayCommand { get; }

        /// <summary>Parameter: the age in days ("7", "14", "28").</summary>
        public ICommand AgeCommand { get; }

        public ICommand AverageDiameterCommand { get; }

        public ICommand AverageHeightCommand { get; }

        public override Task InitializeAsync() => RunAsync(async () =>
        {
            if (_id != null)
            {
                _record = await _services.Specimens.GetAsync(_id.Value) ?? throw new NotFoundException("la probeta");
                Fill(_record, includeLoad: true);

                var decision = _services.Specimens.CanEdit(_record, _services.Session.IsTestUnlocked(_testId));
                if (!decision.IsAllowed)
                {
                    IsMeasurementsReadOnly = true;
                    IsLoadReadOnly = true;
                    Notice = decision.Reason;
                }
                else if (decision.IsRuptureLoadOnly)
                {
                    IsMeasurementsReadOnly = true;
                    Notice = RuptureLoadOnlyNotice;
                }

                return;
            }

            if (_copyFrom != null)
            {
                Fill(_copyFrom, includeLoad: false);
            }
            else
            {
                MoldDate = DateInput.Format(_services.Specimens.Today);
                if (_startFrom != null)
                {
                    Slump = NumberInput.Format(_startFrom.Slump);
                    DiameterCm = NumberInput.Format(_startFrom.DiameterCm);
                    HeightCm = NumberInput.Format(_startFrom.HeightCm);
                    StartNote = $"Slump, diámetro y altura de la probeta Nº {_startFrom.Id}.";
                    OnPropertyChanged(nameof(StartNote));
                }
            }
        });

        protected override async Task SaveCoreAsync()
        {
            if (!CanSave)
            {
                throw new AccessDeniedException(Notice ?? "No se puede modificar esta probeta.");
            }

            var errors = new ValidationErrors();
            var specimen = new Specimen
            {
                Id = _record.Id,
                CompressionTestId = _testId,
                RowVersion = _record.RowVersion,
                IsLocked = _record.IsLocked,
                Code = Code,
                Slump = NumberInput.Parse(nameof(Slump), Slump, errors),
                MoldDate = DateInput.Parse(nameof(MoldDate), MoldDate, errors, required: true) ?? default,
                RuptureDate = DateInput.Parse(nameof(RuptureDate), RuptureDate, errors, required: true) ?? default,
                DiameterCm = NumberInput.Parse(nameof(DiameterCm), DiameterCm, errors, required: true) ?? 0,
                HeightCm = NumberInput.Parse(nameof(HeightCm), HeightCm, errors, required: true) ?? 0,
                WeightKg = NumberInput.Parse(nameof(WeightKg), WeightKg, errors, required: true) ?? 0,
                RuptureLoadTon = NumberInput.Parse(nameof(RuptureLoadTon), RuptureLoadTon, errors),
            };

            if (!string.IsNullOrWhiteSpace(AgeDays) && !TryAge(out _))
            {
                errors.Add(nameof(AgeDays), $"Días entre 0 y {MaxAgeDays}.");
            }

            errors.ThrowIfAny();
            await _services.Specimens.SaveAsync(specimen, _services.Session.IsTestUnlocked(_testId));
            SavedId = specimen.Id;
        }

        private void Fill(Specimen source, bool includeLoad)
        {
            _syncing = true;
            Code = source.Code ?? string.Empty;
            Slump = NumberInput.Format(source.Slump);
            MoldDate = DateInput.Format(source.MoldDate);
            RuptureDate = DateInput.Format(source.RuptureDate);
            AgeDays = source.AgeDays.ToString(CultureInfo.InvariantCulture);
            DiameterCm = NumberInput.Format(source.DiameterCm);
            HeightCm = NumberInput.Format(source.HeightCm);
            WeightKg = NumberInput.Format(source.WeightKg);
            RuptureLoadTon = includeLoad ? NumberInput.Format(source.RuptureLoadTon) : string.Empty;
            _syncing = false;
        }

        private bool TryAge(out int days) =>
            int.TryParse(AgeDays?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out days) && days <= MaxAgeDays;

        private void UpdateRuptureFromAge()
        {
            if (TryAge(out var days) && DateInput.TryParse(MoldDate, out var mold) && mold.HasValue)
            {
                _syncing = true;
                RuptureDate = DateInput.Format(mold.Value.AddDays(days));
                _syncing = false;
            }
        }

        private void UpdateAgeFromDates()
        {
            if (DateInput.TryParse(MoldDate, out var mold) && mold.HasValue
                && DateInput.TryParse(RuptureDate, out var rupture) && rupture.HasValue && rupture.Value >= mold.Value)
            {
                _syncing = true;
                AgeDays = (rupture.Value - mold.Value).Days.ToString(CultureInfo.InvariantCulture);
                _syncing = false;
            }
        }

        private void Average(string title, string label, int decimals, Action<string> apply)
        {
            var calculator = new AverageCalculatorViewModel(title, label, decimals);
            if (_dialogs.Show(calculator) && calculator.Result.HasValue)
            {
                apply(NumberInput.Format(calculator.Result));
            }
        }

        private static decimal Number(string text) => NumberInput.TryParse(text, out var value) ? value ?? 0 : 0;

        private static string FormatPreview(double? value, string unit) =>
            value.HasValue ? value.Value.ToString("0.00", CultureInfo.InvariantCulture) + " " + unit : "—";

        private void RaisePreview()
        {
            OnPropertyChanged(nameof(DensityText));
            OnPropertyChanged(nameof(StrengthText));
            OnPropertyChanged(nameof(StrengthValueText));
            OnPropertyChanged(nameof(PercentText));
        }

        private void RaiseEditState()
        {
            OnPropertyChanged(nameof(CanEditMeasurements));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CloseText));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// "Registrar rotura": the daily job in one small dialog. Shows the probeta read-only, takes only the rupture
    /// load and previews strength and % of fck. "Guardar y siguiente" goes on to the next probeta due.
    /// Follows <see cref="SpecimenEditPolicy"/> like the full form.
    /// </summary>
    public sealed class RuptureEntryViewModel : EditorDialogViewModel
    {
        public const string LockedNote = "Ensayo bloqueado: las medidas no se pueden cambiar.";

        private readonly AppServices _services;
        private readonly int _testId;
        private readonly int _specimenId;
        private readonly decimal _designFck;
        private readonly int? _nextId;
        private Specimen _record;
        private string _ruptureLoadTon = string.Empty;
        private string _notice;
        private string _lockNote;
        private bool _canSave = true;
        private bool _closed;

        public RuptureEntryViewModel(AppServices services, int testId, int specimenId, decimal designFck, int? nextId)
        {
            _services = services;
            _testId = testId;
            _specimenId = specimenId;
            _designFck = designFck;
            _nextId = nextId;
            CloseRequested += accepted => _closed = accepted;
            SaveAndNextCommand = new AsyncRelayCommand(SaveAndNextAsync, () => CanSave && HasNext && !IsBusy);
        }

        public override string Title => $"Registrar rotura · Probeta Nº {_specimenId}";

        public override double PreferredWidth => 560;

        public Specimen Specimen => _record;

        public string MoldDateText => DateInput.Format(_record?.MoldDate);

        /// <summary>"7 días (hoy)".</summary>
        public string AgeText { get; private set; }

        /// <summary>"Romper hoy", "Retrasada", "Mañana", "Pendiente".</summary>
        public string StatusText { get; private set; }

        /// <summary>Chip colour: Overdue, DueSoon or Pending.</summary>
        public SpecimenStatus Status { get; private set; }

        public string RuptureLoadTon
        {
            get => _ruptureLoadTon;
            set
            {
                if (SetProperty(ref _ruptureLoadTon, value))
                {
                    OnPropertyChanged(nameof(StrengthText));
                    OnPropertyChanged(nameof(PercentText));
                }
            }
        }

        public string StrengthText => Strength.HasValue ? Strength.Value.ToString("0.00", CultureInfo.InvariantCulture) : "—";

        public string PercentText
        {
            get
            {
                var percent = SpecimenMath.PercentOfFck(Strength, _designFck);
                return percent.HasValue ? string.Format(CultureInfo.InvariantCulture, "{0:0} %", percent.Value) : "—";
            }
        }

        public string PercentLabel => _designFck > 0
            ? string.Format(CultureInfo.InvariantCulture, "% del fck ({0:0.##})", _designFck)
            : "% del fck";

        /// <summary>Why it can't be recorded (overdue on a locked test…); null when it can.</summary>
        public string Notice
        {
            get => _notice;
            private set => SetProperty(ref _notice, value);
        }

        /// <summary>Quiet line on a locked test: only the load can be typed here anyway.</summary>
        public string LockNote
        {
            get => _lockNote;
            private set => SetProperty(ref _lockNote, value);
        }

        public bool CanSave
        {
            get => _canSave;
            private set
            {
                if (SetProperty(ref _canSave, value))
                {
                    OnPropertyChanged(nameof(CloseText));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string CloseText => CanSave ? "Cancelar" : "Cerrar";

        public bool HasNext => _nextId.HasValue;

        public string NextText => _nextId.HasValue ? $"Guardar y siguiente (Nº {_nextId})" : null;

        /// <summary>Closed with "Guardar y siguiente": the page opens the next probeta due.</summary>
        public bool NextRequested { get; private set; }

        public ICommand SaveAndNextCommand { get; }

        private double? Strength
        {
            get
            {
                if (_record == null || !NumberInput.TryParse(RuptureLoadTon, out var load))
                {
                    return null;
                }

                return SpecimenMath.StrengthKgPerCm2(_record.DiameterCm, load);
            }
        }

        public override Task InitializeAsync() => RunAsync(async () =>
        {
            _record = await _services.Specimens.GetAsync(_specimenId) ?? throw new NotFoundException("la probeta");
            var today = _services.Specimens.Today;
            Status = _record.GetStatus(today);
            StatusText = Status == SpecimenStatus.Overdue ? "Retrasada"
                : Status == SpecimenStatus.DueSoon ? (_record.RuptureDate.Date == today.Date ? "Romper hoy" : "Mañana")
                : Status == SpecimenStatus.Tested ? "Rota" : "Pendiente";
            var when = _record.RuptureDate.Date == today.Date ? "hoy" : DateInput.Format(_record.RuptureDate);
            AgeText = string.Format(CultureInfo.InvariantCulture, "{0} días ({1})", _record.AgeDays, when);
            RuptureLoadTon = NumberInput.Format(_record.RuptureLoadTon);

            var unlocked = _services.Session.IsTestUnlocked(_testId);
            var decision = _services.Specimens.CanEdit(_record, unlocked);
            if (!decision.IsAllowed)
            {
                Notice = decision.Reason;
                CanSave = false;
            }
            else if (!unlocked)
            {
                LockNote = LockedNote;
            }

            OnPropertyChanged(nameof(Specimen));
            OnPropertyChanged(nameof(MoldDateText));
            OnPropertyChanged(nameof(AgeText));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(Status));
        });

        protected override async Task SaveCoreAsync()
        {
            if (!CanSave)
            {
                throw new AccessDeniedException(Notice ?? "No se puede registrar la rotura de esta probeta.");
            }

            if (!NumberInput.TryParse(RuptureLoadTon, out var load))
            {
                throw new ValidationException(nameof(RuptureLoadTon), NumberInput.InvalidMessage);
            }

            if (load == null || load <= 0)
            {
                throw new ValidationException(nameof(RuptureLoadTon), "Ingrese la rotura (mayor que cero).");
            }

            var specimen = _record.Clone();
            specimen.RuptureLoadTon = load;
            await _services.Specimens.SaveAsync(specimen, _services.Session.IsTestUnlocked(_testId));
            SavedId = specimen.Id;
        }

        private async Task SaveAndNextAsync()
        {
            NextRequested = true;
            await ((AsyncRelayCommand)SaveCommand).ExecuteAsync();
            if (!_closed)
            {
                NextRequested = false;
            }
        }
    }

    /// <summary>"Promedio": average of two readings (e.g. two diameters measured crosswise).</summary>
    public sealed class AverageCalculatorViewModel : DialogViewModel
    {
        private readonly int _decimals;
        private string _reading1 = string.Empty;
        private string _reading2 = string.Empty;

        public AverageCalculatorViewModel(string title, string label, int decimals)
        {
            Title = title;
            Label = label;
            _decimals = decimals;
            ApplyCommand = new RelayCommand(Apply);
            CancelCommand = new RelayCommand(() => Close(false));
        }

        public override string Title { get; }

        public override double PreferredWidth => 420;

        public string Label { get; }

        public string Reading1
        {
            get => _reading1;
            set
            {
                if (SetProperty(ref _reading1, value))
                {
                    OnPropertyChanged(nameof(ResultText));
                }
            }
        }

        public string Reading2
        {
            get => _reading2;
            set
            {
                if (SetProperty(ref _reading2, value))
                {
                    OnPropertyChanged(nameof(ResultText));
                }
            }
        }

        /// <summary>Rounded average, or null while a reading is missing or invalid.</summary>
        public decimal? Result =>
            NumberInput.TryParse(Reading1, out var a) && a.HasValue && a > 0 && NumberInput.TryParse(Reading2, out var b) && b.HasValue && b > 0
                ? Math.Round((a.Value + b.Value) / 2, _decimals, MidpointRounding.AwayFromZero)
                : (decimal?)null;

        public string ResultText => Result.HasValue ? NumberInput.Format(Result) : "—";

        public ICommand ApplyCommand { get; }

        public ICommand CancelCommand { get; }

        private void Apply()
        {
            var errors = new Dictionary<string, string>();
            foreach (var reading in new[] { (nameof(Reading1), Reading1), (nameof(Reading2), Reading2) })
            {
                if (!NumberInput.TryParse(reading.Item2, out var value))
                {
                    errors[reading.Item1] = NumberInput.InvalidMessage;
                }
                else if (value == null)
                {
                    errors[reading.Item1] = "Campo obligatorio.";
                }
                else if (value <= 0)
                {
                    errors[reading.Item1] = "Debe ser mayor que cero.";
                }
            }

            Errors.Set(errors);
            if (errors.Count == 0)
            {
                Close(true);
            }
        }
    }

    /// <summary>"Filtrar probetas": mold date range and/or probeta numbers ("1-5, 8").</summary>
    public sealed class SpecimenFilterViewModel : DialogViewModel
    {
        private string _moldDateFrom;
        private string _moldDateTo;
        private string _idRanges;

        public SpecimenFilterViewModel(SpecimenFilter current)
        {
            current = current ?? new SpecimenFilter();
            _moldDateFrom = DateInput.Format(current.MoldDateFrom);
            _moldDateTo = DateInput.Format(current.MoldDateTo);
            _idRanges = FormatRanges(current.IdRanges);
            ApplyCommand = new RelayCommand(Apply);
            CancelCommand = new RelayCommand(() => Close(false));
        }

        public override string Title => "Filtrar probetas";

        public override double PreferredWidth => 460;

        public string MoldDateFrom
        {
            get => _moldDateFrom;
            set => SetProperty(ref _moldDateFrom, value);
        }

        public string MoldDateTo
        {
            get => _moldDateTo;
            set => SetProperty(ref _moldDateTo, value);
        }

        /// <summary>"1-5, 8, 10-12".</summary>
        public string IdRanges
        {
            get => _idRanges;
            set => SetProperty(ref _idRanges, value);
        }

        /// <summary>The filter after "Filtrar".</summary>
        public SpecimenFilter Result { get; private set; }

        public ICommand ApplyCommand { get; }

        public ICommand CancelCommand { get; }

        /// <summary>"1-5, 8".</summary>
        public static string FormatRanges(IReadOnlyList<IdRange> ranges) => string.Join(", ", (ranges ?? Array.Empty<IdRange>())
            .Select(r => r.From == r.To
                ? r.From.ToString(CultureInfo.InvariantCulture)
                : string.Format(CultureInfo.InvariantCulture, "{0}-{1}", r.From, r.To)));

        private void Apply()
        {
            var errors = new ValidationErrors();
            var from = DateInput.Parse(nameof(MoldDateFrom), MoldDateFrom, errors);
            var to = DateInput.Parse(nameof(MoldDateTo), MoldDateTo, errors);
            if (from.HasValue && to.HasValue && to < from)
            {
                errors.Add(nameof(MoldDateTo), "La fecha final es anterior a la inicial.");
            }

            if (!IdRangeParser.TryParse(IdRanges, out var ranges))
            {
                errors.Add(nameof(IdRanges), IdRangeParser.InvalidMessage);
            }

            try
            {
                errors.ThrowIfAny();
            }
            catch (ValidationException ex)
            {
                Errors.Set(ex.Errors);
                return;
            }

            Errors.Clear();
            Result = new SpecimenFilter { MoldDateFrom = from, MoldDateTo = to, IdRanges = ranges };
            Close(true);
        }
    }

    /// <summary>
    /// "Desbloquear": an administrator types their user and password on this PC; the test is then unlocked for
    /// the signed-in user until they lock it again or log out.
    /// </summary>
    public sealed class AdminUnlockViewModel : EditorDialogViewModel
    {
        private readonly AuthService _auth;
        private readonly AppSession _session;
        private readonly int _testId;
        private string _userName = string.Empty;
        private string _password = string.Empty;

        public AdminUnlockViewModel(AuthService auth, AppSession session, int testId)
        {
            _auth = auth;
            _session = session;
            _testId = testId;
        }

        public override string Title => "Desbloquear ensayo";

        public override double PreferredWidth => 420;

        public string Explanation =>
            $"Un administrador debe ingresar su usuario y contraseña para desbloquear el ensayo Nº {_testId} en esta sesión.";

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        protected override async Task SaveCoreAsync()
        {
            var admin = await _auth.VerifyAdministratorAsync(UserName, Password);
            _session.UnlockTest(_testId);
            AppLog.Info($"Test {_testId} unlocked by {admin.UserName} for {_session.CurrentUser?.UserName} on {_session.MachineName}");
            SavedId = _testId;
        }
    }
}
