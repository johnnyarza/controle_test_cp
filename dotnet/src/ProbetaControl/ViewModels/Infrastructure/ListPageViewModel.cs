using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Models;
using ProbetaControl.Services;

namespace ProbetaControl.ViewModels.Infrastructure
{
    /// <summary>
    /// A searchable, paged list with New / Edit / Archive / Restore.
    /// Search runs on the server 300 ms after typing stops; a newer search cancels an older one.
    /// Rows arrive 100 at a time ("Mostrar más"), so huge tables never load whole.
    /// </summary>
    public abstract class ListPageViewModel<T> : PageViewModel
        where T : class
    {
        private const int SearchDelayMs = 300;

        private CancellationTokenSource _loadCts;
        private CancellationTokenSource _searchDelayCts;
        private string _searchText = string.Empty;
        private bool _includeArchived;
        private T _selectedItem;
        private int _totalCount;
        private bool _isLoading;
        private bool _hasLoaded;

        protected ListPageViewModel(string title, IDialogService dialogs, AppSession session)
            : base(title)
        {
            Dialogs = dialogs;
            Session = session;

            RefreshCommand = new AsyncRelayCommand(() => LoadAsync(append: false));
            LoadMoreCommand = new AsyncRelayCommand(() => LoadAsync(append: true), () => HasMore && !IsLoading);
            NewCommand = new AsyncRelayCommand(() => EditAsync(null));
            EditCommand = new AsyncRelayCommand(() => EditAsync(SelectedItem), () => SelectedItem != null);
            DuplicateCommand = new AsyncRelayCommand(
                () => OpenEditorAsync(CreateDuplicate(SelectedItem)), () => SelectedItem != null && SupportsDuplicate);
            ToggleArchiveCommand = new AsyncRelayCommand(ToggleArchiveAsync, () => SelectedItem != null && CanArchive);
            PrintCommand = new RelayCommand(Print, () => SupportsPrint);
        }

        protected IDialogService Dialogs { get; }

        protected AppSession Session { get; }

        public ObservableCollection<T> Items { get; } = new ObservableCollection<T>();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value ?? string.Empty))
                {
                    _ = SearchAfterDelayAsync();
                }
            }
        }

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

        public T SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OnPropertyChanged(nameof(ArchiveText));
                    OnPropertyChanged(nameof(SelectedIsArchived));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            private set => SetProperty(ref _totalCount, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public bool HasMore => Items.Count < TotalCount;

        public string StatusText => TotalCount == 0
            ? string.Empty
            : string.Format(CultureInfo.InvariantCulture, "Mostrando {0} de {1}", Items.Count, TotalCount);

        /// <summary>Shown over the empty grid.</summary>
        public string EmptyText => !_hasLoaded || IsLoading || Items.Count > 0
            ? null
            : string.IsNullOrWhiteSpace(SearchText) ? EmptyListText : $"No hay resultados para \"{SearchText.Trim()}\".";

        /// <summary>"Archivar" or "Restaurar", depending on the selected row.</summary>
        public string ArchiveText => SelectedIsArchived ? "Restaurar" : "Archivar";

        /// <summary>The selected row is archived (archive actions show "Restaurar" and its icon).</summary>
        public bool SelectedIsArchived => SelectedItem != null && IsArchived(SelectedItem);

        /// <summary>Archiving and restoring are for administrators.</summary>
        public bool CanArchive => Session.IsAdmin;

        public string ArchiveToolTip => CanArchive
            ? "Archivar o restaurar el registro seleccionado"
            : "Solo un administrador puede archivar o restaurar";

        public ICommand RefreshCommand { get; }

        public ICommand LoadMoreCommand { get; }

        public ICommand NewCommand { get; }

        public ICommand EditCommand { get; }

        /// <summary>Opens a new record pre-filled from the selected one (pages with <see cref="SupportsDuplicate"/>).</summary>
        public ICommand DuplicateCommand { get; }

        /// <summary>The page offers "Duplicar" (toolbar and right-click menu); it then overrides <see cref="CreateDuplicate"/>.</summary>
        public virtual bool SupportsDuplicate => false;

        public virtual string DuplicateToolTip => "Nuevo registro a partir del seleccionado";

        /// <summary>Text of the toolbar button and menu item that open the selected row ("Abrir" for ensayos).</summary>
        public virtual string EditText => "Editar";

        public ICommand ToggleArchiveCommand { get; }

        /// <summary>"Imprimir" (Ctrl+P): the page's report in the preview window (pages with <see cref="SupportsPrint"/>).</summary>
        public ICommand PrintCommand { get; }

        /// <summary>The page offers a report (toolbar button and Ctrl+P); it then overrides <see cref="Print"/>.</summary>
        public virtual bool SupportsPrint => false;

        public virtual string PrintText => "Imprimir";

        public virtual string PrintToolTip => "Listado con todos los registros que encuentra la búsqueda, no solo los que están a la vista (Ctrl+P)";

        protected abstract string EmptyListText { get; }

        public override Task OnNavigatedToAsync() => LoadAsync(append: false);

        public override void OnNavigatedFrom()
        {
            base.OnNavigatedFrom();
            _searchDelayCts?.Cancel();
            _loadCts?.Cancel();
        }

        protected abstract Task<PagedResult<T>> QueryAsync(ListQuery query, CancellationToken cancellationToken);

        /// <summary>Editor for a new record (<paramref name="item"/> null) or an existing one.</summary>
        protected abstract EditorDialogViewModel CreateEditor(T item);

        protected abstract Task SetArchivedAsync(T item, bool archived);

        protected abstract int GetId(T item);

        protected abstract bool IsArchived(T item);

        /// <summary>e.g. "el cliente \"Constructora Sur\"".</summary>
        protected abstract string Describe(T item);

        /// <summary>Reloads the first page and selects <paramref name="selectId"/> when it is on it.</summary>
        protected async Task LoadAsync(bool append, int? selectId = null)
        {
            _loadCts?.Cancel();
            var cts = new CancellationTokenSource();
            _loadCts = cts;
            IsLoading = true;
            RaiseListState();

            try
            {
                var page = append ? new PageRequest(Items.Count) : PageRequest.First;
                var result = await QueryAsync(new ListQuery
                {
                    Text = SearchText,
                    IncludeArchived = IncludeArchived,
                    Page = page,
                }, cts.Token);

                if (cts.IsCancellationRequested)
                {
                    return;
                }

                var keepSelected = SelectedItem == null ? (int?)null : GetId(SelectedItem);
                if (!append)
                {
                    Items.Clear();
                }

                foreach (var item in result.Items)
                {
                    Items.Add(item);
                }

                TotalCount = result.TotalCount;
                _hasLoaded = true;

                var wanted = selectId ?? keepSelected;
                if (!append || selectId.HasValue)
                {
                    SelectedItem = wanted.HasValue ? Items.FirstOrDefault(i => GetId(i) == wanted.Value) : null;
                }
            }
            catch (OperationCanceledException)
            {
                // A newer search replaced this one, or the page was left.
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

        /// <summary>Reloads the first page (e.g. after a filter changed); errors go to the error dialog.</summary>
        protected async Task ReloadReportingErrorsAsync()
        {
            try
            {
                await LoadAsync(append: false);
            }
            catch (Exception ex)
            {
                ErrorSink.Report(ex);
            }
        }

        private async Task SearchAfterDelayAsync()
        {
            _searchDelayCts?.Cancel();
            var cts = new CancellationTokenSource();
            _searchDelayCts = cts;
            try
            {
                await Task.Delay(SearchDelayMs, cts.Token);
                await LoadAsync(append: false);
            }
            catch (OperationCanceledException)
            {
                // Still typing.
            }
            catch (Exception ex)
            {
                ErrorSink.Report(ex);
            }
        }

        /// <summary>Opens the page's report; only called when <see cref="SupportsPrint"/>.</summary>
        protected virtual void Print() => throw new NotSupportedException();

        /// <summary>
        /// Opens a "Listado de …" in the preview window. It prints what the search and "Mostrar archivados" find
        /// (read again from the database), not only the rows loaded so far.
        /// </summary>
        protected void ShowListReport(string title, DateTime today, Func<ListQuery, CancellationToken, Task<MigraDoc.DocumentObjectModel.Document>> build)
        {
            var query = new ListQuery { Text = SearchText, IncludeArchived = IncludeArchived };
            Dialogs.Show(new Reports.ReportPreviewViewModel(title, Services.Reports.ListReports.FileName(title, today), ct => build(query, ct), Dialogs));
        }

        /// <summary>Editor for a new record copied from <paramref name="item"/>; only called when <see cref="SupportsDuplicate"/>.</summary>
        protected virtual EditorDialogViewModel CreateDuplicate(T item) => throw new NotSupportedException();

        /// <summary>"Nuevo" (item null), "Editar", double-click and Enter. Pages that open a detail page override it.</summary>
        protected virtual Task EditAsync(T item) => OpenEditorAsync(CreateEditor(item));

        private async Task OpenEditorAsync(EditorDialogViewModel editor)
        {
            if (Dialogs.Show(editor))
            {
                await LoadAsync(append: false, selectId: editor.SavedId);
            }
        }

        /// <summary>Why the item can't be archived (Spanish), or null. Checked before asking for confirmation.</summary>
        protected virtual Task<string> GetArchiveBlockerAsync(T item) => Task.FromResult<string>(null);

        private async Task ToggleArchiveAsync()
        {
            var item = SelectedItem;
            var archive = !IsArchived(item);
            if (archive)
            {
                var blocker = await GetArchiveBlockerAsync(item);
                if (blocker != null)
                {
                    Dialogs.Warn("No se puede archivar", blocker);
                    return;
                }
            }

            var confirmed = archive
                ? Dialogs.Confirm("Archivar", $"¿Archivar {Describe(item)}?\n\nDejará de aparecer en las listas, pero no se borra: podrá restaurarlo con \"Mostrar archivados\".", "Archivar", destructive: true)
                : Dialogs.Confirm("Restaurar", $"¿Restaurar {Describe(item)}?", "Restaurar");

            if (!confirmed)
            {
                return;
            }

            try
            {
                await SetArchivedAsync(item, archive);
            }
            catch (InUseException ex)
            {
                // Someone started using it between the check and the confirmation.
                Dialogs.Warn("No se puede archivar", ex.Message);
                return;
            }

            await LoadAsync(append: false, selectId: GetId(item));
        }

        private void RaiseListState()
        {
            OnPropertyChanged(nameof(HasMore));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(EmptyText));
            OnPropertyChanged(nameof(ArchiveText));
            OnPropertyChanged(nameof(SelectedIsArchived));
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
