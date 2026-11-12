using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Catalog
{
    /// <summary>
    /// "Buscar cliente": search active clients on the server (name, phone or e-mail), 100 at a time,
    /// and choose one. Used for the client and the concrete provider of a test and for the test list filter.
    /// </summary>
    public sealed class ClientPickerViewModel : DialogViewModel
    {
        private readonly ClientService _clients;
        private readonly IDialogService _dialogs;
        private CancellationTokenSource _loadCts;
        private CancellationTokenSource _delayCts;
        private string _searchText = string.Empty;
        private Client _selectedItem;
        private int _totalCount;
        private bool _isLoading;
        private bool _hasLoaded;

        /// <param name="allowNew">Shows "Nuevo cliente" (choosing a client for a test, not when filtering).</param>
        public ClientPickerViewModel(ClientService clients, IDialogService dialogs, string title = "Buscar cliente",
            int? currentId = null, bool allowNew = false)
        {
            _clients = clients;
            _dialogs = dialogs;
            CanCreate = allowNew;
            Title = title;
            CurrentId = currentId;
            ChooseCommand = new RelayCommand(() => Close(true), () => SelectedItem != null);
            CancelCommand = new RelayCommand(() => Close(false));
            LoadMoreCommand = new AsyncRelayCommand(() => LoadAsync(append: true), () => HasMore && !IsLoading);
            NewClientCommand = new AsyncRelayCommand(CreateClientAsync, () => CanCreate && !IsLoading);
        }

        public override string Title { get; }

        public override double PreferredWidth => 620;

        /// <summary>Selected again when it is on the first page.</summary>
        public int? CurrentId { get; }

        /// <summary>Server search delay after typing stops; 0 in tests.</summary>
        internal int SearchDelayMs { get; set; } = 300;

        public ObservableCollection<Client> Items { get; } = new ObservableCollection<Client>();

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

        public Client SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>The chosen client after the dialog closed with "Elegir".</summary>
        public Client Chosen => SelectedItem;

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public bool HasMore => Items.Count < _totalCount;

        public string StatusText => _totalCount == 0
            ? string.Empty
            : string.Format(CultureInfo.InvariantCulture, "Mostrando {0} de {1}", Items.Count, _totalCount);

        public string EmptyText => !_hasLoaded || IsLoading || Items.Count > 0
            ? null
            : string.IsNullOrWhiteSpace(SearchText)
                ? (CanCreate ? "Todavía no hay clientes. Use \"Nuevo cliente\" para crear el primero." : "Todavía no hay clientes.")
                : $"No hay clientes que coincidan con \"{SearchText.Trim()}\"."
                  + (CanCreate ? "\nSi es un cliente nuevo, use \"Nuevo cliente\"." : string.Empty);

        public ICommand ChooseCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand LoadMoreCommand { get; }

        /// <summary>"Nuevo cliente": create the client here and choose it at once.</summary>
        public ICommand NewClientCommand { get; }

        public bool CanCreate { get; }

        public override Task InitializeAsync() => LoadAsync(append: false);

        /// <summary>Runs the search now (the text box waits until typing stops).</summary>
        internal Task SearchNowAsync() => LoadAsync(append: false);

        /// <summary>Opens the client form (name pre-filled with the search text); a saved client is chosen at once.</summary>
        private async Task CreateClientAsync()
        {
            var editor = new ClientEditorViewModel(_clients, null) { Name = SearchText.Trim() };
            if (!_dialogs.Show(editor))
            {
                return;
            }

            var created = await _clients.GetAsync(editor.SavedId);
            if (created != null)
            {
                SelectedItem = created;
                Close(true);
            }
        }

        private async Task SearchAfterDelayAsync()
        {
            _delayCts?.Cancel();
            var cts = new CancellationTokenSource();
            _delayCts = cts;
            try
            {
                if (SearchDelayMs > 0)
                {
                    await Task.Delay(SearchDelayMs, cts.Token);
                }

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

        private async Task LoadAsync(bool append)
        {
            _loadCts?.Cancel();
            var cts = new CancellationTokenSource();
            _loadCts = cts;
            IsLoading = true;
            RaiseListState();

            try
            {
                var result = await _clients.SearchAsync(new ListQuery
                {
                    Text = SearchText,
                    Page = append ? new PageRequest(Items.Count) : PageRequest.First,
                }, cts.Token);

                if (cts.IsCancellationRequested)
                {
                    return;
                }

                if (!append)
                {
                    Items.Clear();
                }

                foreach (var client in result.Items)
                {
                    Items.Add(client);
                }

                _totalCount = result.TotalCount;
                _hasLoaded = true;

                if (!append)
                {
                    SelectedItem = null;
                    foreach (var client in Items)
                    {
                        if (client.Id == CurrentId || Items.Count == 1)
                        {
                            SelectedItem = client;
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Replaced by a newer search, or the dialog closed.
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

        private void RaiseListState()
        {
            OnPropertyChanged(nameof(HasMore));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(EmptyText));
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
