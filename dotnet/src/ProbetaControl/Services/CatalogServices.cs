using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ProbetaControl.Data;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    /// <summary>Clients and providers: validation, then the repository.</summary>
    public class ContactService<T>
        where T : ContactRecord
    {
        private static readonly Regex EmailPattern = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly ContactRepository<T> _repository;

        public ContactService(ContactRepository<T> repository)
        {
            _repository = repository;
        }

        public Task<PagedResult<T>> SearchAsync(ListQuery query, CancellationToken cancellationToken = default) =>
            _repository.SearchAsync(query, cancellationToken);

        public Task<T> GetAsync(int id, CancellationToken cancellationToken = default) => _repository.GetAsync(id, cancellationToken);

        public Task SaveAsync(T record, CancellationToken cancellationToken = default)
        {
            var errors = new ValidationErrors()
                .Required("Name", record.Name)
                .MaxLength("Name", record.Name, 150)
                .MaxLength("Phone", record.Phone, 50)
                .MaxLength("Address", record.Address, 200)
                .MaxLength("Email", record.Email, 150);

            if (!string.IsNullOrWhiteSpace(record.Email) && !EmailPattern.IsMatch(record.Email.Trim()))
            {
                errors.Add("Email", "Correo electrónico no válido.");
            }

            errors.ThrowIfAny();
            return record.Id == 0 ? _repository.InsertAsync(record, cancellationToken) : _repository.UpdateAsync(record, cancellationToken);
        }

        public Task SetArchivedAsync(T record, bool archived, CancellationToken cancellationToken = default) =>
            _repository.SetArchivedAsync(record, archived, cancellationToken);
    }

    public sealed class ClientService : ContactService<Client>
    {
        public ClientService(ClientRepository repository)
            : base(repository)
        {
        }
    }

    public sealed class ProviderService : ContactService<Provider>
    {
        public ProviderService(ProviderRepository repository)
            : base(repository)
        {
        }
    }

    public sealed class MaterialService
    {
        private readonly MaterialRepository _repository;

        public MaterialService(MaterialRepository repository)
        {
            _repository = repository;
        }

        public Task<PagedResult<Material>> SearchAsync(ListQuery query, CancellationToken cancellationToken = default) =>
            _repository.SearchAsync(query, cancellationToken);

        public Task<Material> GetAsync(int id, CancellationToken cancellationToken = default) => _repository.GetAsync(id, cancellationToken);

        public Task SaveAsync(Material material, CancellationToken cancellationToken = default)
        {
            new ValidationErrors()
                .Required("Name", material.Name)
                .MaxLength("Name", material.Name, 150)
                .ThrowIfAny();

            return material.Id == 0 ? _repository.InsertAsync(material, cancellationToken) : _repository.UpdateAsync(material, cancellationToken);
        }

        public Task SetArchivedAsync(Material material, bool archived, CancellationToken cancellationToken = default) =>
            _repository.SetArchivedAsync(material, archived, cancellationToken);
    }

    public sealed class ConcreteDesignService
    {
        private readonly ConcreteDesignRepository _repository;

        public ConcreteDesignService(ConcreteDesignRepository repository)
        {
            _repository = repository;
        }

        public Task<PagedResult<ConcreteDesign>> SearchAsync(ListQuery query, CancellationToken cancellationToken = default) =>
            _repository.SearchAsync(query, cancellationToken);

        public Task<ConcreteDesign> GetAsync(int id, CancellationToken cancellationToken = default) => _repository.GetAsync(id, cancellationToken);

        public Task SaveAsync(ConcreteDesign design, CancellationToken cancellationToken = default)
        {
            var errors = new ValidationErrors()
                .Required("Description", design.Description)
                .MaxLength("Description", design.Description, 150);

            if (design.Fck < 0)
            {
                errors.Add("Fck", "El fck no puede ser negativo.");
            }

            if (design.Slump < 0)
            {
                errors.Add("Slump", "El slump no puede ser negativo.");
            }

            var filled = design.Materials.Where(m => m.MaterialId.HasValue).ToList();
            foreach (var slot in design.Materials)
            {
                var field = "Material" + slot.Position;
                if (slot.Position < 1 || slot.Position > ConcreteDesign.MaxMaterials)
                {
                    errors.Add(field, $"Máximo {ConcreteDesign.MaxMaterials} materiales.");
                }
                else if (slot.MaterialId.HasValue && (!slot.QuantityKgPerM3.HasValue || slot.QuantityKgPerM3 < 0))
                {
                    errors.Add(field, "Ingrese la cantidad (kg/m³).");
                }
                else if (!slot.MaterialId.HasValue && slot.QuantityKgPerM3.HasValue)
                {
                    errors.Add(field, "Seleccione el material.");
                }
            }

            foreach (var duplicate in filled.GroupBy(m => m.MaterialId).Where(g => g.Count() > 1).SelectMany(g => g.Skip(1)))
            {
                errors.Add("Material" + duplicate.Position, "Material repetido.");
            }

            errors.ThrowIfAny();
            return design.Id == 0 ? _repository.InsertAsync(design, cancellationToken) : _repository.UpdateAsync(design, cancellationToken);
        }

        public Task SetArchivedAsync(ConcreteDesign design, bool archived, CancellationToken cancellationToken = default) =>
            _repository.SetArchivedAsync(design, archived, cancellationToken);
    }
}
