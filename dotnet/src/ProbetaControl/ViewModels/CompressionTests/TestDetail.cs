using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.CompressionTests
{
    /// <summary>
    /// One ensayo: its header and its probetas (red = overdue, yellow = due today or tomorrow).
    /// Adding, copying and changing probetas needs the test unlocked (administrator, the operator who created it
    /// in this session, or an administrator's "Desbloquear"); on a locked test an operator can only record the
    /// rupture load of a pending probeta. Archiving probetas is for administrators.
    /// </summary>
    public sealed class TestDetailPageViewModel : PageViewModel
    {
        private readonly AppServices _services;
        private readonly IDialogService _dialogs;
        private readonly IPageNavigator _navigator;
        private CancellationTokenSource _loadCts;
        private CompressionTest _test;
        private SpecimenRow _selectedRow;
        private SpecimenFilter _filter = new SpecimenFilter();
        private bool _includeArchived;
        private int _dueCount;
        private bool _isLoading;
        private bool _hasLoaded;

        public TestDetailPageViewModel(AppServices services, IDialogService dialogs, IPageNavigator navigator, int testId)
            : base($"Ensayo Nº {testId}")
        {
            _services = services;
            _dialogs = dialogs;
            _navigator = navigator;
            TestId = testId;

            BackCommand = new RelayCommand(() => _navigator?.Back());
            RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
            EditHeaderCommand = new AsyncRelayCommand(EditHeaderAsync, () => Test != null);
            UnlockCommand = new RelayCommand(Unlock, () => Test != null && !IsUnlocked);
            LockCommand = new RelayCommand(Lock, () => IsUnlocked && !Session.IsAdmin);
            NewSpecimenCommand = new AsyncRelayCommand(
                () => OpenSpecimenAsync(SpecimenEditorViewModel.New(_services, _dialogs, TestId, DesignFck, LastSpecimen())), () => CanAddSpecimens);
            EditSpecimenCommand = new AsyncRelayCommand(
                () => OpenSpecimenAsync(SpecimenEditorViewModel.Edit(_services, _dialogs, TestId, SelectedRow.Id, DesignFck)), () => SelectedRow != null);
            CopySpecimenCommand = new AsyncRelayCommand(
                () => OpenSpecimenAsync(SpecimenEditorViewModel.Copy(_services, _dialogs, TestId, SelectedRow.Specimen, DesignFck)),
                () => SelectedRow != null && CanAddSpecimens);
            ToggleArchiveSpecimenCommand = new AsyncRelayCommand(ToggleArchiveSpecimenAsync, () => SelectedRow != null && Session.IsAdmin);
            FilterCommand = new AsyncRelayCommand(ChooseFilterAsync);
            ClearFilterCommand = new AsyncRelayCommand(() => ApplyFilterAsync(new SpecimenFilter()), () => HasFilter);
            RecordRuptureCommand = new AsyncRelayCommand(() => RecordRuptureAsync(SelectedRow), () => CanRecordRupture(SelectedRow));
            PrintCommand = new RelayCommand(Print, () => Test != null);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync, () => Test != null && !IsBusy);
            OpenSpecimenCommand = new AsyncRelayCommand(
                () => CanRecordRupture(SelectedRow)
                    ? RecordRuptureAsync(SelectedRow)
                    : OpenSpecimenAsync(SpecimenEditorViewModel.Edit(_services, _dialogs, TestId, SelectedRow.Id, DesignFck)),
                () => SelectedRow != null);
        }

        public int TestId { get; }

        /// <summary>"Registrar rotura": the quick dialog for a probeta without a rupture load.</summary>
        public ICommand RecordRuptureCommand { get; }

        /// <summary>Double-click / Enter: "Registrar rotura" for an unbroken probeta, the full form otherwise.</summary>
        public ICommand OpenSpecimenCommand { get; }

        /// <summary>"Administrador", "Desbloqueado" or "Bloqueado" (chip next to the title).</summary>
        public string LockChipText => Session.IsAdmin ? "Administrador" : IsUnlocked ? "Desbloqueado" : "Bloqueado";

        /// <summary>Active probetas of the whole ensayo (indicator tiles ignore the filter).</summary>
        public int ActiveCount { get; private set; }

        public int TestedCount { get; private set; }

        public string TestedText => string.Format(CultureInfo.InvariantCulture, "de {0}", ActiveCount);

        /// <summary>Average strength of the broken probetas (kg/cm²), "—" when none.</summary>
        public string AverageStrengthText { get; private set; } = "—";

        /// <summary>"103 % fck", empty when nothing is broken or the design has no fck.</summary>
        public string AveragePercentText { get; private set; }

        private AppSession Session => _services.Session;

        /// <summary>Header (date, client, provider, design, project, address); null until loaded.</summary>
        public CompressionTest Test
        {
            get => _test;
            private set
            {
                if (SetProperty(ref _test, value))
                {
                    OnPropertyChanged(nameof(TestDateText));
                    OnPropertyChanged(nameof(DesignFck));
                    OnPropertyChanged(nameof(DesignText));
                    RaiseLockState();
                }
            }
        }

        public string TestDateText => DateInput.Format(Test?.TestDate);

        /// <summary>Design strength the results are compared with (kg/cm²).</summary>
        public decimal DesignFck => Test?.ConcreteDesignFck ?? 0;

        /// <summary>"H-21 bombeable · fck 210 kg/cm²".</summary>
        public string DesignText => Test == null ? null
            : string.Format(CultureInfo.InvariantCulture, "{0} · fck {1:0.##} kg/cm²", Test.ConcreteDesignDescription, Test.ConcreteDesignFck);

        public ObservableCollection<SpecimenRow> Rows { get; } = new ObservableCollection<SpecimenRow>();

        public SpecimenRow SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (SetProperty(ref _selectedRow, value))
                {
                    OnPropertyChanged(nameof(ArchiveText));
                    OnPropertyChanged(nameof(SelectedIsArchived));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        /// <summary>Archived probetas shown in grey.</summary>
        public bool IncludeArchived
        {
            get => _includeArchived;
            set
            {
                if (SetProperty(ref _includeArchived, value))
                {
                    _ = ReloadReportingErrorsAsync();
                }
            }
        }

        /// <summary>Untested probetas whose rupture date is today or earlier (whole test, whatever the filter).</summary>
        public int DueCount
        {
            get => _dueCount;
            private set
            {
                if (SetProperty(ref _dueCount, value))
                {
                    OnPropertyChanged(nameof(DueText));
                }
            }
        }

        public string DueText => DueCount == 0
            ? "Ninguna probeta para romper"
            : DueCount == 1 ? "1 probeta para romper" : $"{DueCount} probetas para romper";

        public string CountText => Rows.Count == 1 ? "1 probeta" : $"{Rows.Count} probetas";

        public SpecimenFilter Filter => _filter;

        public bool HasFilter => _filter.MoldDateFrom.HasValue || _filter.MoldDateTo.HasValue || _filter.IdRanges.Count > 0;

        /// <summary>"Moldeo 01/11/2026 – 05/11/2026 · Probetas 1-5, 8".</summary>
        public string FilterText => HasFilter ? Describe(_filter) : null;

        public string EmptyText => !_hasLoaded || IsLoading || Rows.Count > 0
            ? null
            : HasFilter
                ? "Ninguna probeta coincide con el filtro."
                : "Este ensayo todavía no tiene probetas." + (CanAddSpecimens ? " Use \"Nueva probeta\" para agregar la primera." : string.Empty);

        public bool IsUnlocked => Session.IsTestUnlocked(TestId);

        public bool CanAddSpecimens => Test != null && !Test.IsArchived && IsUnlocked;

        public bool ShowUnlock => !IsUnlocked;

        public bool ShowLock => IsUnlocked && !Session.IsAdmin;

        /// <summary>Who may change what, in one line next to the lock button.</summary>
        public string LockText => Session.IsAdmin
            ? "Administrador: puede modificar todo el ensayo."
            : IsUnlocked
                ? "Desbloqueado: puede agregar y modificar probetas."
                : "Bloqueado: solo puede registrar la rotura de probetas pendientes.";

        public bool SelectedIsArchived => SelectedRow?.IsArchived == true;

        public string ArchiveText => SelectedIsArchived ? "Restaurar" : "Archivar";

        public string ArchiveToolTip => Session.IsAdmin
            ? "Archivar o restaurar la probeta seleccionada"
            : "Solo un administrador puede archivar o restaurar probetas";

        public string NewSpecimenToolTip => CanAddSpecimens
            ? "Agregar una probeta a este ensayo"
            : Test?.IsArchived == true
                ? "El ensayo está archivado."
                : "El ensayo está bloqueado. Un administrador debe desbloquearlo.";

        public ICommand BackCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand EditHeaderCommand { get; }

        public ICommand UnlockCommand { get; }

        public ICommand LockCommand { get; }

        public ICommand NewSpecimenCommand { get; }

        public ICommand EditSpecimenCommand { get; }

        public ICommand CopySpecimenCommand { get; }

        public ICommand ToggleArchiveSpecimenCommand { get; }

        public ICommand FilterCommand { get; }

        public ICommand ClearFilterCommand { get; }

        /// <summary>"Imprimir" (Ctrl+P): the ensayo report in the preview window.</summary>
        public ICommand PrintCommand { get; }

        /// <summary>"Excel" (Ctrl+E): the probetas the page shows (never archived ones) as an .xlsx file.</summary>
        public ICommand ExportExcelCommand { get; }

        public string ExportExcelToolTip => HasFilter
            ? "Guardar un archivo de Excel con las probetas del filtro (sin las archivadas) (Ctrl+E)"
            : "Guardar un archivo de Excel con todas las probetas del ensayo (sin las archivadas) (Ctrl+E)";

        public string PrintToolTip => HasFilter
            ? "Informe del ensayo con las probetas del filtro (sin las archivadas) (Ctrl+P)"
            : "Informe del ensayo con todas sus probetas (sin las archivadas) (Ctrl+P)";

        public override Task OnNavigatedToAsync() => LoadAsync();

        /// <summary>The report prints what the grid shows (same filter), never archived probetas; data is read fresh.</summary>
        private void Print()
        {
            var filter = _filter;
            var title = $"Informe del ensayo Nº {TestId}";
            _dialogs.Show(new Reports.ReportPreviewViewModel(title, Services.Reports.ReportService.TestFileName(Test),
                ct => _services.Reports.BuildTestReportAsync(TestId, filter, ct), _dialogs));
        }

        /// <summary>
        /// Asks where to save, then writes the workbook with what the grid shows (same filter, data read fresh).
        /// A file that can't be written (open in Excel, read-only folder) is explained, not an error.
        /// </summary>
        private async Task ExportExcelAsync()
        {
            var name = Reports.ReportPreviewViewModel.SafeFileName(Services.Reports.ReportService.TestFileName(Test));
            var path = _dialogs.ChooseSaveFile("Exportar a Excel", Services.Reports.TestExcelExport.FileFilter, name);
            if (path == null)
            {
                return;
            }

            var filter = _filter;
            try
            {
                await RunBusyAsync("Exportando a Excel…",
                    ct => _services.Reports.ExportTestToExcelAsync(TestId, filter, path, ct), cancellable: false);
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
            {
                AppLog.Error("Exportar a Excel", ex);
                _dialogs.Warn("No se pudo guardar el archivo",
                    "No se pudo guardar \"" + System.IO.Path.GetFileName(path) + "\". Si está abierto en Excel, ciérrelo y vuelva a exportar."
                    + "\n\n" + ex.Message);
                return;
            }

            _dialogs.Warn("Exportar a Excel", "Archivo guardado:\n" + path);
        }

        public override void OnNavigatedFrom()
        {
            base.OnNavigatedFrom();
            _loadCts?.Cancel();
        }

        /// <summary>Loads the header, the probetas (with the filter) and the due count; keeps or selects a row.</summary>
        public async Task LoadAsync(int? selectId = null)
        {
            _loadCts?.Cancel();
            var cts = new CancellationTokenSource();
            _loadCts = cts;
            IsLoading = true;
            RaiseListState();

            try
            {
                var test = await _services.Tests.GetAsync(TestId, cts.Token) ?? throw new NotFoundException("el ensayo");
                var filter = new SpecimenFilter
                {
                    MoldDateFrom = _filter.MoldDateFrom,
                    MoldDateTo = _filter.MoldDateTo,
                    IdRanges = _filter.IdRanges,
                    IncludeArchived = IncludeArchived,
                };
                var specimens = await _services.Specimens.ListByTestAsync(TestId, filter, cts.Token);
                var whole = HasFilter || IncludeArchived
                    ? await _services.Specimens.ListByTestAsync(TestId, null, cts.Token)
                    : specimens;
                var due = await _services.Specimens.CountDueAsync(TestId, cts.Token);
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                Test = test;
                DueCount = due;
                UpdateIndicators(whole.Where(s => !s.IsArchived).ToList(), test.ConcreteDesignFck);
                var keep = selectId ?? SelectedRow?.Id;
                var today = _services.Specimens.Today;
                Rows.Clear();
                foreach (var specimen in specimens)
                {
                    Rows.Add(new SpecimenRow(specimen, today, test.ConcreteDesignFck));
                }

                _hasLoaded = true;
                SelectedRow = keep.HasValue ? Rows.FirstOrDefault(r => r.Id == keep.Value) : null;
            }
            catch (OperationCanceledException)
            {
                // Left the page, or a newer load started.
            }
            finally
            {
                if (ReferenceEquals(_loadCts, cts))
                {
                    IsLoading = false;
                    RaiseListState();
                }
            }
        }

        private void UpdateIndicators(System.Collections.Generic.IReadOnlyList<Specimen> active, decimal fck)
        {
            var strengths = active.Where(s => s.StrengthKgPerCm2.HasValue).Select(s => s.StrengthKgPerCm2.Value).ToList();
            ActiveCount = active.Count;
            TestedCount = strengths.Count;
            double? average = strengths.Count > 0 ? strengths.Average() : (double?)null;
            AverageStrengthText = average.HasValue ? average.Value.ToString("0.00", CultureInfo.InvariantCulture) : "—";
            var percent = SpecimenMath.PercentOfFck(average, fck);
            AveragePercentText = percent.HasValue ? string.Format(CultureInfo.InvariantCulture, "{0:0} % fck", percent.Value) : null;
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(TestedCount));
            OnPropertyChanged(nameof(TestedText));
            OnPropertyChanged(nameof(AverageStrengthText));
            OnPropertyChanged(nameof(AveragePercentText));
        }

        /// <summary>A probeta still to break (no load yet, not archived).</summary>
        private static bool CanRecordRupture(SpecimenRow row) => row != null && !row.IsArchived && row.RuptureLoadTon == null;

        /// <summary>
        /// "Registrar rotura", then — with "Guardar y siguiente" — the next probeta due today or earlier,
        /// so the day's probetas are broken one after another without going back to the grid.
        /// </summary>
        private async Task RecordRuptureAsync(SpecimenRow row)
        {
            while (row != null)
            {
                var next = NextDue(row.Id);
                var dialog = new RuptureEntryViewModel(_services, TestId, row.Id, DesignFck, next?.Id);
                if (!_dialogs.Show(dialog))
                {
                    return;
                }

                await LoadAsync(dialog.SavedId);
                row = dialog.NextRequested && next != null ? Rows.FirstOrDefault(r => r.Id == next.Id) : null;
                if (row != null)
                {
                    SelectedRow = row;
                }
            }
        }

        /// <summary>The unbroken probeta due soonest (today or earlier), other than <paramref name="currentId"/>.</summary>
        private SpecimenRow NextDue(int currentId)
        {
            var today = _services.Specimens.Today;
            return Rows
                .Where(r => r.Id != currentId && CanRecordRupture(r) && r.RuptureDate.Date <= today.Date)
                .OrderBy(r => r.RuptureDate)
                .ThenBy(r => r.Id)
                .FirstOrDefault();
        }

        private async Task ReloadReportingErrorsAsync()
        {
            try
            {
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ErrorSink.Report(ex);
            }
        }

        private async Task EditHeaderAsync()
        {
            var editor = new TestEditorViewModel(_services.Tests, _services.Clients, _services.Designs, Session, _dialogs, TestId);
            if (_dialogs.Show(editor))
            {
                await LoadAsync();
            }
        }

        private void Unlock()
        {
            if (_dialogs.Show(new AdminUnlockViewModel(_services.Auth, Session, TestId)))
            {
                RaiseLockState();
            }
        }

        private void Lock()
        {
            Session.LockTest(TestId);
            RaiseLockState();
        }

        /// <summary>The newest active probeta: "Nueva probeta" starts from its slump, diameter and height.</summary>
        private Specimen LastSpecimen() => Rows.Where(r => !r.IsArchived).OrderByDescending(r => r.Id).FirstOrDefault()?.Specimen;

        private async Task OpenSpecimenAsync(SpecimenEditorViewModel editor)
        {
            if (_dialogs.Show(editor))
            {
                await LoadAsync(editor.SavedId);
            }
        }

        private async Task ToggleArchiveSpecimenAsync()
        {
            var row = SelectedRow;
            var archive = !row.IsArchived;
            var confirmed = archive
                ? _dialogs.Confirm("Archivar", $"¿Archivar la probeta Nº {row.Id}?\n\nDejará de aparecer en el ensayo y en los informes, pero no se borra: podrá restaurarla con \"Mostrar archivadas\".", "Archivar", destructive: true)
                : _dialogs.Confirm("Restaurar", $"¿Restaurar la probeta Nº {row.Id}?", "Restaurar");
            if (!confirmed)
            {
                return;
            }

            await _services.Specimens.SetArchivedAsync(row.Specimen, archive);
            await LoadAsync(row.Id);
        }

        private async Task ChooseFilterAsync()
        {
            var dialog = new SpecimenFilterViewModel(_filter);
            if (_dialogs.Show(dialog))
            {
                await ApplyFilterAsync(dialog.Result);
            }
        }

        private async Task ApplyFilterAsync(SpecimenFilter filter)
        {
            _filter = filter;
            OnPropertyChanged(nameof(Filter));
            OnPropertyChanged(nameof(HasFilter));
            OnPropertyChanged(nameof(FilterText));
            OnPropertyChanged(nameof(PrintToolTip));
            OnPropertyChanged(nameof(ExportExcelToolTip));
            await LoadAsync();
        }

        private static string Describe(SpecimenFilter filter)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (filter.MoldDateFrom.HasValue || filter.MoldDateTo.HasValue)
            {
                parts.Add("Moldeo " + (filter.MoldDateFrom.HasValue ? DateInput.Format(filter.MoldDateFrom) : "…")
                          + " – " + (filter.MoldDateTo.HasValue ? DateInput.Format(filter.MoldDateTo) : "…"));
            }

            if (filter.IdRanges.Count > 0)
            {
                parts.Add("Probetas " + SpecimenFilterViewModel.FormatRanges(filter.IdRanges));
            }

            return string.Join(" · ", parts);
        }

        private void RaiseLockState()
        {
            OnPropertyChanged(nameof(IsUnlocked));
            OnPropertyChanged(nameof(CanAddSpecimens));
            OnPropertyChanged(nameof(ShowUnlock));
            OnPropertyChanged(nameof(ShowLock));
            OnPropertyChanged(nameof(LockText));
            OnPropertyChanged(nameof(LockChipText));
            OnPropertyChanged(nameof(NewSpecimenToolTip));
            OnPropertyChanged(nameof(EmptyText));
            CommandManager.InvalidateRequerySuggested();
        }

        private void RaiseListState()
        {
            OnPropertyChanged(nameof(CountText));
            OnPropertyChanged(nameof(EmptyText));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>One probeta in the grid, with its calculated values and status (row colour) for today.</summary>
    public sealed class SpecimenRow
    {
        public SpecimenRow(Specimen specimen, DateTime today, decimal designFck = 0)
        {
            Specimen = specimen;
            RowStatus = specimen.GetStatus(today);
            StatusText = StatusLabel(RowStatus, specimen.RuptureDate, today);
            PercentOfFck = SpecimenMath.PercentOfFck(specimen.StrengthKgPerCm2, designFck);
        }

        /// <summary>Strength / design fck × 100; null until broken.</summary>
        public double? PercentOfFck { get; }

        public Specimen Specimen { get; }

        public int Id => Specimen.Id;

        public string Code => Specimen.Code;

        public decimal? Slump => Specimen.Slump;

        public DateTime MoldDate => Specimen.MoldDate;

        public DateTime RuptureDate => Specimen.RuptureDate;

        public int AgeDays => Specimen.AgeDays;

        public decimal DiameterCm => Specimen.DiameterCm;

        public decimal HeightCm => Specimen.HeightCm;

        public decimal WeightKg => Specimen.WeightKg;

        public double? DensityKgPerM3 => Specimen.DensityKgPerM3;

        public decimal? RuptureLoadTon => Specimen.RuptureLoadTon;

        public double? StrengthKgPerCm2 => Specimen.StrengthKgPerCm2;

        public bool IsArchived => Specimen.IsArchived;

        /// <summary>Row colour: Overdue (red), DueSoon (yellow), Archived (grey).</summary>
        public SpecimenStatus RowStatus { get; }

        /// <summary>"Retrasada", "Hoy", "Mañana", "Pendiente", "Rota", "Archivada".</summary>
        public string StatusText { get; }

        /// <summary>Accessible name of the row.</summary>
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "Probeta {0}, {1}", Id, StatusText);

        private static string StatusLabel(SpecimenStatus status, DateTime rupture, DateTime today)
        {
            switch (status)
            {
                case SpecimenStatus.Overdue:
                    return "Retrasada";
                case SpecimenStatus.DueSoon:
                    return rupture.Date == today.Date ? "Hoy" : "Mañana";
                case SpecimenStatus.Tested:
                    return "Rota";
                case SpecimenStatus.Archived:
                    return "Archivada";
                default:
                    return "Pendiente";
            }
        }
    }
}
