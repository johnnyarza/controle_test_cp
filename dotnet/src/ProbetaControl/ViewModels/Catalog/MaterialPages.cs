using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Services.Reports;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Catalog
{
    public sealed class MaterialsPageViewModel : ListPageViewModel<Material>
    {
        private readonly MaterialService _materials;
        private readonly ProviderService _providers;
        private readonly ReportService _reports;

        public MaterialsPageViewModel(MaterialService materials, ProviderService providers, ReportService reports, IDialogService dialogs, AppSession session)
            : base("Materiales", dialogs, session)
        {
            _reports = reports;
            _materials = materials;
            _providers = providers;
        }

        protected override string EmptyListText => "Todavía no hay materiales. Use \"Nuevo\" para agregar el primero.";

        protected override Task<PagedResult<Material>> QueryAsync(ListQuery query, CancellationToken cancellationToken) =>
            _materials.SearchAsync(query, cancellationToken);

        protected override EditorDialogViewModel CreateEditor(Material item) => new MaterialEditorViewModel(_materials, _providers, item?.Id);

        protected override Task SetArchivedAsync(Material item, bool archived) => _materials.SetArchivedAsync(item, archived);

        protected override Task<string> GetArchiveBlockerAsync(Material item) => _materials.GetArchiveBlockerAsync(item);

        protected override int GetId(Material item) => item.Id;

        protected override bool IsArchived(Material item) => item.IsArchived;

        protected override string Describe(Material item) => $"el material \"{item.Name}\"";

        public override bool SupportsPrint => true;

        protected override void Print() => ShowListReport(ListReports.MaterialsTitle, _reports.Today, _reports.BuildMaterialListAsync);
    }

    public sealed class MaterialEditorViewModel : EditorDialogViewModel
    {
        /// <summary>Stand-in for "no provider" in the combo box.</summary>
        public static readonly Provider NoProvider = new Provider { Id = 0, Name = "(Sin proveedor)" };

        private readonly MaterialService _materials;
        private readonly ProviderService _providers;
        private readonly int? _id;
        private Material _record = new Material();
        private string _name = string.Empty;
        private string _materialType = MaterialTypes.Other;
        private Provider _provider = NoProvider;

        public MaterialEditorViewModel(MaterialService materials, ProviderService providers, int? id)
        {
            _materials = materials;
            _providers = providers;
            _id = id;
        }

        public override string Title => _id == null ? "Nuevo material" : "Editar material";

        public ObservableCollection<Provider> Providers { get; } = new ObservableCollection<Provider> { NoProvider };

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public IReadOnlyList<MaterialTypeOption> MaterialTypeOptions => MaterialTypes.All;

        /// <summary>One of <see cref="MaterialTypes"/> (combo box SelectedValue).</summary>
        public string MaterialType
        {
            get => _materialType;
            set => SetProperty(ref _materialType, value);
        }

        public Provider Provider
        {
            get => _provider;
            set => SetProperty(ref _provider, value ?? NoProvider);
        }

        public override Task InitializeAsync() => RunAsync(async () =>
        {
            // Active providers only (the list is short); an archived provider still shows if this material uses it.
            var providers = await _providers.SearchAsync(new ListQuery { Page = new PageRequest(0, PageRequest.MaxSize) });
            foreach (var provider in providers.Items)
            {
                Providers.Add(provider);
            }

            if (_id == null)
            {
                return;
            }

            _record = await _materials.GetAsync(_id.Value) ?? throw new NotFoundException("el material");
            Name = _record.Name;
            MaterialType = _record.MaterialType;
            if (_record.ProviderId.HasValue)
            {
                var current = Providers.FirstOrDefault(p => p.Id == _record.ProviderId.Value);
                if (current == null)
                {
                    current = new Provider { Id = _record.ProviderId.Value, Name = _record.ProviderName + " (archivado)" };
                    Providers.Add(current);
                }

                Provider = current;
            }
        });

        protected override async Task SaveCoreAsync()
        {
            _record.Name = Name;
            _record.MaterialType = MaterialType;
            _record.ProviderId = Provider == NoProvider ? (int?)null : Provider.Id;
            await _materials.SaveAsync(_record);
            SavedId = _record.Id;
        }
    }
}
