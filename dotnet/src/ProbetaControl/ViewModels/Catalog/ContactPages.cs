using System.Threading;
using System.Threading.Tasks;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Catalog
{
    /// <summary>Shared list page for clients and providers (same columns).</summary>
    public abstract class ContactListPageViewModel<T> : ListPageViewModel<T>
        where T : ContactRecord, new()
    {
        private readonly ContactService<T> _service;

        protected ContactListPageViewModel(string title, ContactService<T> service, IDialogService dialogs, AppSession session)
            : base(title, dialogs, session)
        {
            _service = service;
        }

        protected ContactService<T> Service => _service;

        protected override Task<PagedResult<T>> QueryAsync(ListQuery query, CancellationToken cancellationToken) =>
            _service.SearchAsync(query, cancellationToken);

        protected override Task SetArchivedAsync(T item, bool archived) => _service.SetArchivedAsync(item, archived);

        protected override Task<string> GetArchiveBlockerAsync(T item) => _service.GetArchiveBlockerAsync(item);

        protected override int GetId(T item) => item.Id;

        protected override bool IsArchived(T item) => item.IsArchived;
    }

    public sealed class ClientsPageViewModel : ContactListPageViewModel<Client>
    {
        public ClientsPageViewModel(ClientService service, IDialogService dialogs, AppSession session)
            : base("Clientes", service, dialogs, session)
        {
        }

        protected override string EmptyListText => "Todavía no hay clientes. Use \"Nuevo\" para agregar el primero.";

        protected override EditorDialogViewModel CreateEditor(Client item) => new ClientEditorViewModel(Service, item?.Id);

        protected override string Describe(Client item) => $"el cliente \"{item.Name}\"";
    }

    public sealed class ProvidersPageViewModel : ContactListPageViewModel<Provider>
    {
        public ProvidersPageViewModel(ProviderService service, IDialogService dialogs, AppSession session)
            : base("Proveedores", service, dialogs, session)
        {
        }

        protected override string EmptyListText => "Todavía no hay proveedores. Use \"Nuevo\" para agregar el primero.";

        protected override EditorDialogViewModel CreateEditor(Provider item) => new ProviderEditorViewModel(Service, item?.Id);

        protected override string Describe(Provider item) => $"el proveedor \"{item.Name}\"";
    }

    /// <summary>Edit form for a client or provider. Loads a fresh copy when it opens.</summary>
    public abstract class ContactEditorViewModel<T> : EditorDialogViewModel
        where T : ContactRecord, new()
    {
        private readonly ContactService<T> _service;
        private readonly int? _id;
        private T _record = new T();
        private string _name = string.Empty;
        private string _phone = string.Empty;
        private string _address = string.Empty;
        private string _email = string.Empty;

        protected ContactEditorViewModel(ContactService<T> service, int? id)
        {
            _service = service;
            _id = id;
        }

        public bool IsNew => _id == null;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public override async Task InitializeAsync()
        {
            if (_id == null)
            {
                return;
            }

            await RunAsync(async () =>
            {
                _record = await _service.GetAsync(_id.Value) ?? throw new NotFoundException("el registro");
                Name = _record.Name;
                Phone = _record.Phone;
                Address = _record.Address;
                Email = _record.Email;
            });
        }

        protected override async Task SaveCoreAsync()
        {
            _record.Name = Name;
            _record.Phone = Phone;
            _record.Address = Address;
            _record.Email = Email;
            await _service.SaveAsync(_record);
            SavedId = _record.Id;
        }
    }

    public sealed class ClientEditorViewModel : ContactEditorViewModel<Client>
    {
        public ClientEditorViewModel(ContactService<Client> service, int? id)
            : base(service, id)
        {
        }

        public override string Title => IsNew ? "Nuevo cliente" : "Editar cliente";
    }

    public sealed class ProviderEditorViewModel : ContactEditorViewModel<Provider>
    {
        public ProviderEditorViewModel(ContactService<Provider> service, int? id)
            : base(service, id)
        {
        }

        public override string Title => IsNew ? "Nuevo proveedor" : "Editar proveedor";
    }
}
