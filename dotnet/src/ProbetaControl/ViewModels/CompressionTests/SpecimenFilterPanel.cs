using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// The panel under "Filtrar" on the ensayo page (layout D chosen by the user): Estado, the ensayo's own mold days,
    /// a date range on the mold or rupture date (with Hoy / Esta semana / Próx. 7 días), ages, numbers or description
    /// and archived probetas. Every option shows how many probetas it has, and "Mostrar N probetas" counts what the
    /// choices keep, all from the probetas the page already loaded (<see cref="SpecimenFilter.Matches"/>).
    /// Nothing changes on the page until "Mostrar".
    /// </summary>
    public sealed class SpecimenFilterPanelViewModel : EditorViewModel
    {
        private readonly IReadOnlyList<Specimen> _all;
        private readonly DateTime _today;
        private readonly Func<SpecimenFilter, Task> _apply;
        private readonly Action _cancel;
        private bool _rangeOnRupture;
        private string _dateFrom = string.Empty;
        private string _dateTo = string.Empty;
        private string _numbersOrText = string.Empty;
        private bool _includeArchived;
        private bool _updating;

        /// <param name="all">Every probeta of the ensayo, archived ones included.</param>
        public SpecimenFilterPanelViewModel(IReadOnlyList<Specimen> all, DateTime today, SpecimenFilter current, bool includeArchived,
            Func<SpecimenFilter, Task> apply, Action cancel)
        {
            _all = all ?? Array.Empty<Specimen>();
            _today = today.Date;
            _apply = apply;
            _cancel = cancel;
            current = current ?? SpecimenFilter.None;

            States = new[]
            {
                new FilterOption<SpecimenStateFilter>(SpecimenStateFilter.All, "Todas"),
                new FilterOption<SpecimenStateFilter>(SpecimenStateFilter.Untested, "Sin romper"),
                new FilterOption<SpecimenStateFilter>(SpecimenStateFilter.Due, "Para romper (hoy o antes)"),
                new FilterOption<SpecimenStateFilter>(SpecimenStateFilter.Tested, "Rotas"),
            };
            MoldDays = _all.GroupBy(s => s.MoldDate.Date).OrderBy(g => g.Key)
                .Select(g => new FilterOption<DateTime>(g.Key, DateInput.Format(g.Key), DayDescription(g)))
                .ToList();
            Ages = _all.Select(s => s.AgeDays).Distinct().OrderBy(a => a)
                .Select(a => new FilterOption<int>(a, a.ToString(CultureInfo.InvariantCulture) + " d"))
                .ToList();

            // Days and ages the ensayo no longer has (e.g. a filter kept after archiving) are simply dropped.
            _updating = true;
            foreach (var option in States)
            {
                option.IsChecked = option.Value == current.State;
            }

            foreach (var option in MoldDays)
            {
                option.IsChecked = current.MoldDays.Any(d => d.Date == option.Value);
            }

            foreach (var option in Ages)
            {
                option.IsChecked = current.Ages.Contains(option.Value);
            }

            _rangeOnRupture = current.DateField == SpecimenDateField.Rupture;
            _dateFrom = DateInput.Format(current.DateFrom);
            _dateTo = DateInput.Format(current.DateTo);
            _numbersOrText = current.IdRanges.Count > 0 ? IdRangeParser.Format(current.IdRanges) : current.CodeText ?? string.Empty;
            _includeArchived = includeArchived;
            _updating = false;

            foreach (var option in States.Cast<INotifyPropertyChanged>().Concat(MoldDays).Concat(Ages))
            {
                option.PropertyChanged += OnOptionChanged;
            }

            ApplyCommand = new AsyncRelayCommand(ApplyAsync);
            CancelCommand = new RelayCommand(() => _cancel?.Invoke());
            ClearCommand = new RelayCommand(ClearChoices);
            AllAgesCommand = new RelayCommand(() => SetAll(Ages, false));
            TodayCommand = new RelayCommand(() => SetRange(_today, _today));
            ThisWeekCommand = new RelayCommand(() =>
            {
                var monday = _today.AddDays(-(((int)_today.DayOfWeek + 6) % 7));
                SetRange(monday, monday.AddDays(6));
            });
            NextSevenDaysCommand = new RelayCommand(() => SetRange(_today, _today.AddDays(6)));
            Recount();
        }

        public IReadOnlyList<FilterOption<SpecimenStateFilter>> States { get; }

        /// <summary>The ensayo's mold days, oldest first: "03/11/2026 · Losa eje 1-2 · 6".</summary>
        public IReadOnlyList<FilterOption<DateTime>> MoldDays { get; }

        public bool HasMoldDays => MoldDays.Count > 0;

        /// <summary>The ages the ensayo has ("7 d", "14 d", "28 d").</summary>
        public IReadOnlyList<FilterOption<int>> Ages { get; }

        /// <summary>"Todas" lit when no age is picked.</summary>
        public bool AllAges => Ages.All(a => !a.IsChecked);

        /// <summary>The "Entre fechas" range uses the rupture date (else the mold date).</summary>
        public bool RangeOnRupture
        {
            get => _rangeOnRupture;
            set
            {
                if (SetProperty(ref _rangeOnRupture, value))
                {
                    OnPropertyChanged(nameof(RangeOnMold));
                    Recount();
                }
            }
        }

        public bool RangeOnMold
        {
            get => !_rangeOnRupture;
            set => RangeOnRupture = !value;
        }

        public string DateFrom
        {
            get => _dateFrom;
            set
            {
                if (SetProperty(ref _dateFrom, value ?? string.Empty))
                {
                    Errors.Clear(nameof(DateFrom));
                    Recount();
                }
            }
        }

        public string DateTo
        {
            get => _dateTo;
            set
            {
                if (SetProperty(ref _dateTo, value ?? string.Empty))
                {
                    Errors.Clear(nameof(DateTo));
                    Recount();
                }
            }
        }

        /// <summary>"341-345, 350" (numbers) or any other text (part of the description).</summary>
        public string NumbersOrText
        {
            get => _numbersOrText;
            set
            {
                if (SetProperty(ref _numbersOrText, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(NumbersOrTextHint));
                    Recount();
                }
            }
        }

        /// <summary>How the box is read: "Números 341-345, 350" or "Descripción que contenga «columna»".</summary>
        public string NumbersOrTextHint
        {
            get
            {
                var (ranges, text) = ParseNumbersOrText(NumbersOrText);
                return ranges.Count > 0
                    ? "Números " + IdRangeParser.Format(ranges)
                    : text != null ? $"Descripción que contenga «{text}»" : "Vacío = todas.";
            }
        }

        public bool IncludeArchived
        {
            get => _includeArchived;
            set
            {
                if (SetProperty(ref _includeArchived, value))
                {
                    Recount();
                }
            }
        }

        /// <summary>How many probetas the choices keep.</summary>
        public int MatchCount { get; private set; }

        /// <summary>"Mostrar 3 probetas".</summary>
        public string ApplyText => MatchCount == 1 ? "Mostrar 1 probeta" : $"Mostrar {MatchCount} probetas";

        public ICommand ApplyCommand { get; }

        public ICommand CancelCommand { get; }

        /// <summary>"Limpiar": every choice back to "all" (applied with "Mostrar").</summary>
        public ICommand ClearCommand { get; }

        public ICommand AllAgesCommand { get; }

        public ICommand TodayCommand { get; }

        public ICommand ThisWeekCommand { get; }

        public ICommand NextSevenDaysCommand { get; }

        /// <summary>
        /// The box read as ID ranges when it parses as numbers ("345", "341-345, 350"), else as part of the
        /// description ("C-3", "columna").
        /// </summary>
        public static (IReadOnlyList<IdRange> Ranges, string Text) ParseNumbersOrText(string value)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                return (Array.Empty<IdRange>(), null);
            }

            return text.Any(char.IsDigit) && IdRangeParser.TryParse(text, out var ranges) && ranges.Count > 0
                ? (ranges, null)
                : (Array.Empty<IdRange>(), text);
        }

        /// <summary>The filter the choices make; null (with the errors shown) when a date is not valid.</summary>
        public SpecimenFilter BuildFilter()
        {
            var errors = new ValidationErrors();
            var from = DateInput.Parse(nameof(DateFrom), DateFrom, errors);
            var to = DateInput.Parse(nameof(DateTo), DateTo, errors);
            if (from.HasValue && to.HasValue && to < from)
            {
                errors.Add(nameof(DateTo), "La fecha final es anterior a la inicial.");
            }

            try
            {
                errors.ThrowIfAny();
            }
            catch (ValidationException ex)
            {
                Errors.Set(ex.Errors);
                return null;
            }

            Errors.Clear();
            return Compose(from, to);
        }

        private SpecimenFilter Compose(DateTime? from, DateTime? to)
        {
            var (ranges, text) = ParseNumbersOrText(NumbersOrText);
            return new SpecimenFilter
            {
                State = States.FirstOrDefault(s => s.IsChecked)?.Value ?? SpecimenStateFilter.All,
                MoldDays = MoldDays.Where(d => d.IsChecked).Select(d => d.Value).ToList(),
                DateField = RangeOnRupture ? SpecimenDateField.Rupture : SpecimenDateField.Mold,
                DateFrom = from,
                DateTo = to,
                Ages = Ages.Where(a => a.IsChecked).Select(a => a.Value).ToList(),
                IdRanges = ranges,
                CodeText = text,
                IncludeArchived = IncludeArchived,
            };
        }

        private async Task ApplyAsync()
        {
            var filter = BuildFilter();
            if (filter != null && _apply != null)
            {
                await _apply(filter);
            }
        }

        private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_updating || e.PropertyName != nameof(FilterOption<int>.IsChecked))
            {
                return;
            }

            // Estado is one choice: checking one unchecks the others (radio buttons in the view do the same).
            if (sender is FilterOption<SpecimenStateFilter> state && state.IsChecked)
            {
                _updating = true;
                foreach (var other in States.Where(s => !ReferenceEquals(s, state)))
                {
                    other.IsChecked = false;
                }

                _updating = false;
            }

            OnPropertyChanged(nameof(AllAges));
            Recount();
        }

        private void SetRange(DateTime from, DateTime to)
        {
            DateFrom = DateInput.Format(from);
            DateTo = DateInput.Format(to);
        }

        private void ClearChoices()
        {
            _updating = true;
            foreach (var state in States)
            {
                state.IsChecked = state.Value == SpecimenStateFilter.All;
            }

            SetAll(MoldDays, false);
            SetAll(Ages, false);
            _updating = false;
            DateFrom = string.Empty;
            DateTo = string.Empty;
            NumbersOrText = string.Empty;
            IncludeArchived = false;
            Errors.Clear();
            OnPropertyChanged(nameof(AllAges));
            Recount();
        }

        private void SetAll<T>(IEnumerable<FilterOption<T>> options, bool value)
        {
            var was = _updating;
            _updating = true;
            foreach (var option in options)
            {
                option.IsChecked = value;
            }

            _updating = was;
            if (!was)
            {
                OnPropertyChanged(nameof(AllAges));
                Recount();
            }
        }

        /// <summary>Counts per option (whole ensayo, archived ones when ticked) and what the choices keep.</summary>
        private void Recount()
        {
            if (_updating)
            {
                return;
            }

            var pool = _all.Where(s => IncludeArchived || !s.IsArchived).ToList();
            foreach (var state in States)
            {
                var only = new SpecimenFilter { State = state.Value, IncludeArchived = true };
                state.Count = pool.Count(s => only.Matches(s, _today));
            }

            foreach (var day in MoldDays)
            {
                day.Count = pool.Count(s => s.MoldDate.Date == day.Value);
            }

            foreach (var age in Ages)
            {
                age.Count = pool.Count(s => s.AgeDays == age.Value);
            }

            // Dates still being typed count as empty here; "Mostrar" checks them.
            DateInput.TryParse(DateFrom, out var from);
            DateInput.TryParse(DateTo, out var to);
            var filter = Compose(from, to);
            MatchCount = _all.Count(s => filter.Matches(s, _today));
            OnPropertyChanged(nameof(MatchCount));
            OnPropertyChanged(nameof(ApplyText));
        }

        /// <summary>"Losa eje 1-2", or "Losa eje 1-2 (+1)" when the day has other descriptions too.</summary>
        private static string DayDescription(IEnumerable<Specimen> day)
        {
            var codes = day.Select(s => s.Code).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
            return codes.Count == 0 ? string.Empty : codes.Count == 1 ? codes[0] : $"{codes[0]} (+{codes.Count - 1})";
        }
    }

    /// <summary>One part of the active filter above the probetas table ("Sin romper ×").</summary>
    public sealed class FilterChip
    {
        public FilterChip(string text, ICommand removeCommand)
        {
            Text = text;
            RemoveCommand = removeCommand;
        }

        public string Text { get; }

        /// <summary>The × : shows the list without this part.</summary>
        public ICommand RemoveCommand { get; }

        public override string ToString() => Text;
    }

    /// <summary>One choice of the filter panel with its count.</summary>
    public sealed class FilterOption<T> : ObservableObject
    {
        private bool _isChecked;
        private int _count;

        public FilterOption(T value, string text, string detail = null)
        {
            Value = value;
            Text = text;
            Detail = detail;
        }

        public T Value { get; }

        public string Text { get; }

        /// <summary>Muted text after the option (the day's description).</summary>
        public string Detail { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        /// <summary>Accessible name: "Sin romper, 7".</summary>
        public override string ToString() => $"{Text}, {Count}";
    }
}
