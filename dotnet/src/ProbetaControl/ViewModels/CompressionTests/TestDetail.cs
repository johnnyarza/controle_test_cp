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
        private System.Collections.Generic.IReadOnlyList<Specimen> _allSpecimens = Array.Empty<Specimen>();
        private SpecimenFilterPanelViewModel _filterPanel;
        private int _totalCount;
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
            FilterCommand = new RelayCommand(ToggleFilterPanel, () => _hasLoaded);
            ClearFilterCommand = new AsyncRelayCommand(() => ApplyFilterAsync(new SpecimenFilter()), () => HasFilter || IncludeArchived);
            RecordRuptureCommand = new AsyncRelayCommand(() => RecordRuptureAsync(SelectedRow), () => CanRecordRupture(SelectedRow));
            PrintCommand = new RelayCommand(Print, () => Test != null);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync, () => Test != null && !IsBusy);
            LabelsCommand = new RelayCommand(() => PrintLabels(null), () => Test != null);
            SpecimenLabelCommand = new RelayCommand(() => PrintLabels(SelectedRow), () => Test != null && SelectedRow != null && !SelectedRow.Specimen.IsArchived);
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

        public string TestedText => string.Format(AppCulture.Display, "de {0:#,##0}", ActiveCount);

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
            : string.Format(AppCulture.Display, "{0} · fck {1:#,##0.##} kg/cm²", Test.ConcreteDesignDescription, Test.ConcreteDesignFck);

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

        /// <summary>Archived probetas shown in grey ("Incluir archivadas" in the filter panel; never printed).</summary>
        public bool IncludeArchived
        {
            get => _includeArchived;
            set
            {
                if (SetProperty(ref _includeArchived, value))
                {
                    RaiseFilterState();
                    _ = ReloadReportingErrorsAsync();
                }
            }
        }

        /// <summary>The panel under "Filtrar" while it is open, else null.</summary>
        public SpecimenFilterPanelViewModel FilterPanel
        {
            get => _filterPanel;
            private set
            {
                if (SetProperty(ref _filterPanel, value))
                {
                    OnPropertyChanged(nameof(IsFilterOpen));
                }
            }
        }

        /// <summary>Bound to the panel's popup: closing it (click outside, Esc) drops the choices not shown yet.</summary>
        public bool IsFilterOpen
        {
            get => _filterPanel != null;
            set
            {
                if (value && _filterPanel == null)
                {
                    ToggleFilterPanel();
                }
                else if (!value)
                {
                    FilterPanel = null;
                }
            }
        }

        /// <summary>One removable chip per part of the active filter, "Con archivadas" included.</summary>
        public ObservableCollection<FilterChip> FilterChips { get; } = new ObservableCollection<FilterChip>();

        public bool ShowFilterChips => FilterChips.Count > 0;

        /// <summary>"Mostrando 3 de 12:" in front of the chips.</summary>
        public string FilterSummary => $"Mostrando {Rows.Count} de {_totalCount}:";

        /// <summary>"Filtrar" or "Filtrar (2)" with the number of active chips.</summary>
        public string FilterButtonText => FilterChips.Count == 0 ? "Filtrar" : $"Filtrar ({FilterChips.Count})";

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

        /// <summary>Something narrows the list (what Imprimir, Excel and Etiquetas then print); archived ones don't count.</summary>
        public bool HasFilter => _filter.HasCriteria;

        /// <summary>The chips in one line: "Sin romper · Rotura del 20/11/2026 al 27/11/2026"; null without chips.</summary>
        public string FilterText => FilterChips.Count == 0 ? null : string.Join(" · ", FilterChips.Select(c => c.Text));

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

        /// <summary>"Etiquetas" (Ctrl+L): QR labels for the probetas the page shows (not archived; by default only unbroken ones).</summary>
        public ICommand LabelsCommand { get; }

        /// <summary>Right-click "Imprimir etiqueta": the label of one probeta, e.g. to replace a damaged one.</summary>
        public ICommand SpecimenLabelCommand { get; }

        public string LabelsToolTip => HasFilter
            ? "Etiquetas QR para pegar en las probetas del filtro (Ctrl+L)"
            : "Etiquetas QR para pegar en las probetas del ensayo (Ctrl+L)";

        public override Task OnNavigatedToAsync() => LoadAsync();

        /// <summary>
        /// Asks which probetas and where on the sheet to start, then opens the labels in the preview. The labels are
        /// laid out from fresh data with the same filter as the grid (or for the one probeta chosen).
        /// </summary>
        private void PrintLabels(SpecimenRow single)
        {
            var candidates = single != null
                ? new[] { single.Specimen }
                : Rows.Select(r => r.Specimen).Where(s => !s.IsArchived).ToArray();
            var scope = single != null
                ? $"Probeta Nº {single.Id} del ensayo Nº {TestId}"
                : HasFilter ? $"Ensayo Nº {TestId} · probetas del filtro" : $"Ensayo Nº {TestId} · todas sus probetas";
            var options = new LabelOptionsViewModel(_services.LabelSettings, candidates, scope, single != null,
                brokenChosen: single == null && _filter.State == SpecimenStateFilter.Tested);
            if (!_dialogs.Show(options))
            {
                return;
            }

            var filter = single != null ? new SpecimenFilter { IdRanges = new[] { new IdRange(single.Id, single.Id) } } : _filter;
            var onlyUntested = options.OnlyUntested;
            var start = options.StartPosition;
            var title = single != null ? $"Etiqueta de la probeta Nº {single.Id}" : $"Etiquetas del ensayo Nº {TestId}";
            _dialogs.Show(new Reports.ReportPreviewViewModel(title, Services.Reports.ReportService.TestFileName(Test) + " - etiquetas",
                ct => _services.Reports.BuildLabelsAsync(TestId, filter, onlyUntested, start, ct), _dialogs));
        }

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

            _dialogs.Inform("Exportar a Excel", "Archivo guardado:\n" + path);
        }

        public override void OnNavigatedFrom()
        {
            base.OnNavigatedFrom();
            _loadCts?.Cancel();
            FilterPanel = null;
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
                // The whole ensayo once (archived ones too): the filter, the tiles and the filter panel's counts all
                // work from it with SpecimenFilter.Matches, the same rule the printouts use.
                var all = await _services.Specimens.ListByTestAsync(TestId, new SpecimenFilter { IncludeArchived = true }, cts.Token);
                var due = await _services.Specimens.CountDueAsync(TestId, cts.Token);
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                Test = test;
                DueCount = due;
                _allSpecimens = all;
                var active = all.Where(s => !s.IsArchived).ToList();
                UpdateIndicators(active, test.ConcreteDesignFck);
                var keep = selectId ?? SelectedRow?.Id;
                var today = _services.Specimens.Today;
                var shown = _filter.WithArchived(IncludeArchived);
                _totalCount = IncludeArchived ? all.Count : active.Count;
                Rows.Clear();
                foreach (var specimen in all.Where(s => shown.Matches(s, today)))
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
            AverageStrengthText = average.HasValue ? average.Value.ToString("#,##0.00", AppCulture.Display) : "—";
            var percent = SpecimenMath.PercentOfFck(average, fck);
            AveragePercentText = percent.HasValue ? string.Format(AppCulture.Display, "{0:#,##0} % fck", percent.Value) : null;
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

        /// <summary>"Filtrar": opens the panel with the active filter, or closes it (dropping what wasn't shown).</summary>
        private void ToggleFilterPanel()
        {
            FilterPanel = FilterPanel != null
                ? null
                : new SpecimenFilterPanelViewModel(_allSpecimens, _services.Specimens.Today, _filter, IncludeArchived,
                    async filter =>
                    {
                        FilterPanel = null;
                        await ApplyFilterAsync(filter);
                    },
                    () => FilterPanel = null);
        }

        /// <summary>Shows <paramref name="filter"/> (its IncludeArchived sets "Incluir archivadas").</summary>
        private async Task ApplyFilterAsync(SpecimenFilter filter)
        {
            _filter = filter.WithArchived(false);
            _includeArchived = filter.IncludeArchived;
            OnPropertyChanged(nameof(IncludeArchived));
            RaiseFilterState();
            await LoadAsync();
        }

        private void RaiseFilterState()
        {
            FilterChips.Clear();
            foreach (var chip in BuildChips())
            {
                FilterChips.Add(chip);
            }

            OnPropertyChanged(nameof(Filter));
            OnPropertyChanged(nameof(HasFilter));
            OnPropertyChanged(nameof(FilterText));
            OnPropertyChanged(nameof(ShowFilterChips));
            OnPropertyChanged(nameof(FilterButtonText));
            OnPropertyChanged(nameof(PrintToolTip));
            OnPropertyChanged(nameof(ExportExcelToolTip));
            OnPropertyChanged(nameof(LabelsToolTip));
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>The active filter as chips; each one's × shows the list without that part.</summary>
        private System.Collections.Generic.IEnumerable<FilterChip> BuildChips()
        {
            var f = _filter;
            FilterChip Chip(string text, Action<SpecimenFilter> remove) => new FilterChip(text, new AsyncRelayCommand(() =>
            {
                var next = f.WithArchived(IncludeArchived);
                remove(next);
                return ApplyFilterAsync(next);
            }));

            if (f.State != SpecimenStateFilter.All)
            {
                var text = f.State == SpecimenStateFilter.Untested ? "Sin romper"
                    : f.State == SpecimenStateFilter.Due ? "Para romper (hoy o antes)" : "Rotas";
                yield return Chip(text, n => n.State = SpecimenStateFilter.All);
            }

            if (f.MoldDays.Count > 0)
            {
                yield return Chip("Moldeo " + string.Join(", ", f.MoldDays.OrderBy(d => d).Select(d => DateInput.Format(d))),
                    n => n.MoldDays = Array.Empty<DateTime>());
            }

            if (f.DateFrom.HasValue || f.DateTo.HasValue)
            {
                yield return Chip(DescribeRange(f), n =>
                {
                    n.DateFrom = null;
                    n.DateTo = null;
                });
            }

            if (f.Ages.Count > 0)
            {
                yield return Chip("Edad " + string.Join(", ", f.Ages.OrderBy(a => a)) + " d", n => n.Ages = Array.Empty<int>());
            }

            if (f.IdRanges.Count > 0)
            {
                yield return Chip("Nº " + IdRangeParser.Format(f.IdRanges), n => n.IdRanges = Array.Empty<IdRange>());
            }

            if (!string.IsNullOrWhiteSpace(f.CodeText))
            {
                yield return Chip($"Descripción «{f.CodeText.Trim()}»", n => n.CodeText = null);
            }

            if (IncludeArchived)
            {
                yield return Chip("Con archivadas", n => n.IncludeArchived = false);
            }
        }

        /// <summary>"Rotura del 20/11/2026 al 27/11/2026", "Moldeo desde 01/11/2026", "Rotura el 20/11/2026".</summary>
        internal static string DescribeRange(SpecimenFilter filter)
        {
            var field = filter.DateField == SpecimenDateField.Rupture ? "Rotura" : "Moldeo";
            var from = DateInput.Format(filter.DateFrom);
            var to = DateInput.Format(filter.DateTo);
            if (filter.DateFrom.HasValue && filter.DateTo.HasValue)
            {
                return filter.DateFrom.Value.Date == filter.DateTo.Value.Date ? $"{field} el {from}" : $"{field} del {from} al {to}";
            }

            return filter.DateFrom.HasValue ? $"{field} desde {from}" : $"{field} hasta {to}";
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
            OnPropertyChanged(nameof(FilterSummary));
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
