using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Catalog;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.CompressionTests
{
    /// <summary>
    /// Ensayos, the start page: newest first, with the number of specimens to break today or earlier.
    /// Filters: free text (number, client, project, address), one client, and "only with specimens to break".
    /// </summary>
    public sealed class TestsPageViewModel : ListPageViewModel<CompressionTestListItem>
    {
        private readonly CompressionTestService _tests;
        private readonly ClientService _clients;
        private readonly ConcreteDesignService _designs;
        private Client _clientFilter;
        private bool _onlyDue;
        private int _dueTestCount;

        public TestsPageViewModel(CompressionTestService tests, ClientService clients, ConcreteDesignService designs,
            IDialogService dialogs, AppSession session)
            : base("Ensayos", dialogs, session)
        {
            _tests = tests;
            _clients = clients;
            _designs = designs;

            ChooseClientFilterCommand = new RelayCommand(ChooseClientFilter);
            ClearClientFilterCommand = new RelayCommand(() => ClientFilter = null, () => ClientFilter != null);
        }

        /// <summary>Only this client's tests; null for all.</summary>
        public Client ClientFilter
        {
            get => _clientFilter;
            set
            {
                if (SetProperty(ref _clientFilter, value))
                {
                    OnPropertyChanged(nameof(ClientFilterText));
                    OnPropertyChanged(nameof(HasClientFilter));
                    CommandManager.InvalidateRequerySuggested();
                    _ = ReloadReportingErrorsAsync();
                }
            }
        }

        public bool HasClientFilter => ClientFilter != null;

        public string ClientFilterText => ClientFilter?.Name ?? "Todos los clientes";

        public bool OnlyDue
        {
            get => _onlyDue;
            set
            {
                if (SetProperty(ref _onlyDue, value))
                {
                    _ = ReloadReportingErrorsAsync();
                }
            }
        }

        /// <summary>Active tests with specimens to break today or earlier (whatever the other filters).</summary>
        public int DueTestCount
        {
            get => _dueTestCount;
            private set
            {
                if (SetProperty(ref _dueTestCount, value))
                {
                    OnPropertyChanged(nameof(OnlyDueText));
                }
            }
        }

        public string OnlyDueText => string.Format(CultureInfo.InvariantCulture, "Solo con probetas para romper ({0})", DueTestCount);

        public ICommand ChooseClientFilterCommand { get; }

        public ICommand ClearClientFilterCommand { get; }

        protected override string EmptyListText => ClientFilter != null || OnlyDue
            ? "No hay ensayos con estos filtros."
            : "Todavía no hay ensayos. Use \"Nuevo\" para crear el primero.";

        protected override async Task<PagedResult<CompressionTestListItem>> QueryAsync(ListQuery query, CancellationToken cancellationToken)
        {
            if (query.Page.Offset == 0)
            {
                DueTestCount = await _tests.CountWithDueAsync(cancellationToken);
            }

            return await _tests.SearchAsync(new CompressionTestQuery
            {
                Text = query.Text,
                IncludeArchived = query.IncludeArchived,
                Page = query.Page,
                ClientId = ClientFilter?.Id,
                OnlyDue = OnlyDue,
            }, cancellationToken);
        }

        protected override EditorDialogViewModel CreateEditor(CompressionTestListItem item) =>
            new TestEditorViewModel(_tests, _clients, _designs, Session, Dialogs, item?.Id);

        protected override async Task SetArchivedAsync(CompressionTestListItem item, bool archived)
        {
            var test = await _tests.GetAsync(item.Id) ?? throw new NotFoundException("el ensayo");
            await _tests.SetArchivedAsync(test, archived, Session.IsTestUnlocked(item.Id));
        }

        protected override int GetId(CompressionTestListItem item) => item.Id;

        protected override bool IsArchived(CompressionTestListItem item) => item.IsArchived;

        protected override string Describe(CompressionTestListItem item) => $"el ensayo Nº {item.Id} ({item.ProjectName})";

        private void ChooseClientFilter()
        {
            var picker = new ClientPickerViewModel(_clients, Dialogs, "Filtrar por cliente", ClientFilter?.Id);
            if (Dialogs.Show(picker) && picker.Chosen != null)
            {
                ClientFilter = picker.Chosen;
            }
        }
    }

    /// <summary>
    /// New test, or the header of an existing one: date, client, concrete provider, design, project and address.
    /// An existing test can be changed only when it is unlocked for the user (<see cref="AppSession.IsTestUnlocked"/>);
    /// otherwise the form is read-only.
    /// </summary>
    public sealed class TestEditorViewModel : EditorDialogViewModel
    {
        public const string LockedMessage = "El ensayo está bloqueado: solo un administrador puede modificar sus datos.";

        private readonly CompressionTestService _tests;
        private readonly ClientService _clients;
        private readonly ConcreteDesignService _designs;
        private readonly AppSession _session;
        private readonly IDialogService _dialogs;
        private readonly int? _id;
        private CompressionTest _record = new CompressionTest();
        private string _testDate = string.Empty;
        private int? _clientId;
        private string _clientName = string.Empty;
        private int? _concreteProviderId;
        private string _concreteProviderName = string.Empty;
        private int? _concreteDesignId;
        private string _projectName = string.Empty;
        private string _address = string.Empty;
        private bool _isReadOnly;

        public TestEditorViewModel(CompressionTestService tests, ClientService clients, ConcreteDesignService designs,
            AppSession session, IDialogService dialogs, int? id)
        {
            _tests = tests;
            _clients = clients;
            _designs = designs;
            _session = session;
            _dialogs = dialogs;
            _id = id;

            ChooseClientCommand = new RelayCommand(ChooseClient, () => CanEdit && !IsBusy);
            ChooseConcreteProviderCommand = new RelayCommand(ChooseConcreteProvider, () => CanEdit && !IsBusy);
            UseClientAsProviderCommand = new RelayCommand(
                () => SetConcreteProvider(_clientId, _clientName),
                () => CanEdit && !IsBusy && _clientId != null && _clientId != _concreteProviderId);
        }

        public override string Title => _id == null ? "Nuevo ensayo" : $"Ensayo Nº {_id}";

        public override double PreferredWidth => 620;

        public bool IsNew => _id == null;

        /// <summary>Test date, dd/mm/aaaa.</summary>
        public string TestDate
        {
            get => _testDate;
            set => SetProperty(ref _testDate, value);
        }

        /// <summary>Chosen with "Buscar…"; named like the field so it shows the error.</summary>
        public string ClientName
        {
            get => _clientName;
            private set => SetProperty(ref _clientName, value);
        }

        public string ConcreteProviderName
        {
            get => _concreteProviderName;
            private set => SetProperty(ref _concreteProviderName, value);
        }

        /// <summary>Active designs (plus the test's own design if it was archived later).</summary>
        public ObservableCollection<ConcreteDesign> Designs { get; } = new ObservableCollection<ConcreteDesign>();

        public int? ConcreteDesignId
        {
            get => _concreteDesignId;
            set => SetProperty(ref _concreteDesignId, value);
        }

        /// <summary>Obra / proyecto.</summary>
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        /// <summary>Ubicación.</summary>
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            private set
            {
                if (SetProperty(ref _isReadOnly, value))
                {
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(CloseText));
                    OnPropertyChanged(nameof(LockWarning));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool CanEdit => !IsReadOnly;

        public string LockWarning => IsReadOnly ? LockedMessage : null;

        public string CloseText => IsReadOnly ? "Cerrar" : "Cancelar";

        public ICommand ChooseClientCommand { get; }

        public ICommand ChooseConcreteProviderCommand { get; }

        /// <summary>"Igual que el cliente": the client also supplies the concrete.</summary>
        public ICommand UseClientAsProviderCommand { get; }

        public override Task InitializeAsync() => RunAsync(async () =>
        {
            var designs = await _designs.SearchAsync(new ListQuery { Page = new PageRequest(0, PageRequest.MaxSize) });
            foreach (var design in designs.Items)
            {
                Designs.Add(design);
            }

            if (_id == null)
            {
                TestDate = DateInput.Format(_tests.Today);
                return;
            }

            _record = await _tests.GetAsync(_id.Value) ?? throw new NotFoundException("el ensayo");
            IsReadOnly = !_session.IsTestUnlocked(_id.Value);

            TestDate = DateInput.Format(_record.TestDate);
            _clientId = _record.ClientId;
            ClientName = _record.ClientName;
            SetConcreteProvider(_record.ConcreteProviderId, _record.ConcreteProviderName);
            ProjectName = _record.ProjectName;
            Address = _record.Address;

            if (Designs.All(d => d.Id != _record.ConcreteDesignId))
            {
                // A design archived after the test was created stays on this test only.
                Designs.Add(new ConcreteDesign
                {
                    Id = _record.ConcreteDesignId,
                    Description = _record.ConcreteDesignDescription + " (archivada)",
                    IsArchived = true,
                });
            }

            ConcreteDesignId = _record.ConcreteDesignId;
        });

        protected override async Task SaveCoreAsync()
        {
            if (IsReadOnly)
            {
                throw new AccessDeniedException(LockedMessage);
            }

            var errors = new ValidationErrors();
            _record.TestDate = DateInput.Parse("TestDate", TestDate, errors, required: true) ?? default;
            _record.ClientId = _clientId ?? 0;
            _record.ConcreteProviderId = _concreteProviderId ?? 0;
            _record.ConcreteDesignId = ConcreteDesignId ?? 0;
            _record.ProjectName = ProjectName;
            _record.Address = Address;

            CompressionTestService.Validate(_record, errors)
                .Rename("ClientId", nameof(ClientName))
                .Rename("ConcreteProviderId", nameof(ConcreteProviderName))
                .ThrowIfAny();

            var isNew = _record.Id == 0;
            await _tests.SaveAsync(_record, testUnlocked: isNew || _session.IsTestUnlocked(_record.Id));
            if (isNew)
            {
                // The operator who creates a test may keep working on it during this session.
                _session.UnlockTest(_record.Id);
            }

            SavedId = _record.Id;
        }

        private void ChooseClient()
        {
            var picker = new ClientPickerViewModel(_clients, _dialogs, "Elegir cliente", _clientId, allowNew: true);
            if (_dialogs.Show(picker) && picker.Chosen != null)
            {
                _clientId = picker.Chosen.Id;
                ClientName = picker.Chosen.Name;
                Errors.Clear(nameof(ClientName));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ChooseConcreteProvider()
        {
            var picker = new ClientPickerViewModel(_clients, _dialogs, "Elegir proveedor del hormigón", _concreteProviderId, allowNew: true);
            if (_dialogs.Show(picker) && picker.Chosen != null)
            {
                SetConcreteProvider(picker.Chosen.Id, picker.Chosen.Name);
            }
        }

        private void SetConcreteProvider(int? id, string name)
        {
            _concreteProviderId = id;
            ConcreteProviderName = name ?? string.Empty;
            Errors.Clear(nameof(ConcreteProviderName));
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
