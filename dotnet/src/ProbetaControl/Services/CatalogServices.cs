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
        private readonly AppSession _session;

        public ContactService(ContactRepository<T> repository, AppSession session)
        {
            _repository = repository;
            _session = session;
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

        /// <summary>
        /// Administrators only. Archiving is refused (<see cref="InUseException"/>) while active records use the record.
        /// </summary>
        public async Task SetArchivedAsync(T record, bool archived, CancellationToken cancellationToken = default)
        {
            _session.RequireAdmin(AppSession.ArchiveAdminOnly);
            var blocker = archived ? await GetArchiveBlockerAsync(record, cancellationToken).ConfigureAwait(false) : null;
            if (blocker != null)
            {
                throw new InUseException(blocker);
            }

            await _repository.SetArchivedAsync(record, archived, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Why the record can't be archived (Spanish), or null when it can.</summary>
        public virtual Task<string> GetArchiveBlockerAsync(T record, CancellationToken cancellationToken = default) =>
            Task.FromResult<string>(null);
    }

    public sealed class ClientService : ContactService<Client>
    {
        private readonly ClientRepository _repository;

        public ClientService(ClientRepository repository, AppSession session)
            : base(repository, session)
        {
            _repository = repository;
        }

        public override async Task<string> GetArchiveBlockerAsync(Client record, CancellationToken cancellationToken = default)
        {
            var usage = await _repository.GetUsageAsync(record.Id, cancellationToken).ConfigureAwait(false);
            return usage.IsInUse
                ? InUseException.BuildMessage($"el cliente \"{record.Name}\"", usage, "ensayo activo", "ensayos activos",
                    "Archive primero esos ensayos.")
                : null;
        }
    }

    public sealed class ProviderService : ContactService<Provider>
    {
        private readonly ProviderRepository _repository;

        public ProviderService(ProviderRepository repository, AppSession session)
            : base(repository, session)
        {
            _repository = repository;
        }

        public override async Task<string> GetArchiveBlockerAsync(Provider record, CancellationToken cancellationToken = default)
        {
            var usage = await _repository.GetUsageAsync(record.Id, cancellationToken).ConfigureAwait(false);
            return usage.IsInUse
                ? InUseException.BuildMessage($"el proveedor \"{record.Name}\"", usage, "material activo", "materiales activos",
                    "Archive primero esos materiales o asígneles otro proveedor.")
                : null;
        }
    }

    public sealed class MaterialService
    {
        private readonly MaterialRepository _repository;
        private readonly AppSession _session;

        public MaterialService(MaterialRepository repository, AppSession session)
        {
            _repository = repository;
            _session = session;
        }

        public Task<PagedResult<Material>> SearchAsync(ListQuery query, CancellationToken cancellationToken = default) =>
            _repository.SearchAsync(query, cancellationToken);

        public Task<Material> GetAsync(int id, CancellationToken cancellationToken = default) => _repository.GetAsync(id, cancellationToken);

        public Task SaveAsync(Material material, CancellationToken cancellationToken = default)
        {
            var errors = new ValidationErrors()
                .Required("Name", material.Name)
                .MaxLength("Name", material.Name, 150);
            if (!MaterialTypes.IsValid(material.MaterialType))
            {
                errors.Add("MaterialType", "Elija el tipo de material.");
            }

            errors.ThrowIfAny();
            return material.Id == 0 ? _repository.InsertAsync(material, cancellationToken) : _repository.UpdateAsync(material, cancellationToken);
        }

        /// <summary>
        /// Administrators only. Archiving is refused (<see cref="InUseException"/>) while an active design uses the material.
        /// </summary>
        public async Task SetArchivedAsync(Material material, bool archived, CancellationToken cancellationToken = default)
        {
            _session.RequireAdmin(AppSession.ArchiveAdminOnly);
            var blocker = archived ? await GetArchiveBlockerAsync(material, cancellationToken).ConfigureAwait(false) : null;
            if (blocker != null)
            {
                throw new InUseException(blocker);
            }

            await _repository.SetArchivedAsync(material, archived, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Why the material can't be archived (Spanish), or null when it can.</summary>
        public async Task<string> GetArchiveBlockerAsync(Material material, CancellationToken cancellationToken = default)
        {
            var usage = await _repository.GetUsageAsync(material.Id, cancellationToken).ConfigureAwait(false);
            return usage.IsInUse
                ? InUseException.BuildMessage($"el material \"{material.Name}\"", usage, "dosificación activa", "dosificaciones activas",
                    "Cambie el material en esas dosificaciones o archívelas primero.")
                : null;
        }
    }

    public sealed class ConcreteDesignService
    {
        public const string InUseEditDenied = "Solo un administrador puede modificar una dosificación que ya se usa en ensayos.";

        private readonly ConcreteDesignRepository _repository;
        private readonly AppSession _session;

        public ConcreteDesignService(ConcreteDesignRepository repository, AppSession session)
        {
            _repository = repository;
            _session = session;
        }

        /// <summary>All tests made with the design (archived ones included).</summary>
        public Task<int> CountTestsAsync(int designId, CancellationToken cancellationToken = default) =>
            _repository.CountTestsAsync(designId, cancellationToken);

        /// <summary>Operators may edit a design only while no test uses it (its reports would change).</summary>
        public bool CanEdit(int testCount) => testCount == 0 || _session.IsAdmin;

        public Task<PagedResult<ConcreteDesign>> SearchAsync(ListQuery query, CancellationToken cancellationToken = default) =>
            _repository.SearchAsync(query, cancellationToken);

        public Task<ConcreteDesign> GetAsync(int id, CancellationToken cancellationToken = default) => _repository.GetAsync(id, cancellationToken);

        public async Task SaveAsync(ConcreteDesign design, CancellationToken cancellationToken = default)
        {
            Validate(design).ThrowIfAny();
            if (design.Id == 0)
            {
                await _repository.InsertAsync(design, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!CanEdit(await CountTestsAsync(design.Id, cancellationToken).ConfigureAwait(false)))
            {
                throw new AccessDeniedException(InUseEditDenied);
            }

            await _repository.UpdateAsync(design, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// The rules of a design. Errors are keyed Description, Fck, Slump, Materials (none at all) and
        /// Material1..Material8. Pass <paramref name="errors"/> to add to errors the form already found
        /// (a field keeps its first error).
        /// </summary>
        public static ValidationErrors Validate(ConcreteDesign design, ValidationErrors errors = null)
        {
            // Upper limits are the DECIMAL column sizes, so a typo gives a message instead of a database error.
            const decimal MaxFck = 1000000m;
            const decimal MaxSlump = 10000m;
            const decimal MaxQuantity = 10000000m;

            errors = (errors ?? new ValidationErrors())
                .Required("Description", design.Description)
                .MaxLength("Description", design.Description, 150);

            if (design.Fck < 0)
            {
                errors.Add("Fck", "El fck no puede ser negativo.");
            }
            else if (design.Fck >= MaxFck)
            {
                errors.Add("Fck", "Valor demasiado grande.");
            }

            if (design.Slump < 0)
            {
                errors.Add("Slump", "El slump no puede ser negativo.");
            }
            else if (design.Slump >= MaxSlump)
            {
                errors.Add("Slump", "Valor demasiado grande.");
            }

            var filled = design.Materials.Where(m => m.MaterialId.HasValue).ToList();
            if (filled.Count == 0)
            {
                errors.Add("Materials", "La dosificación debe tener al menos un material.");
            }

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
                else if (slot.QuantityKgPerM3 >= MaxQuantity)
                {
                    errors.Add(field, "Valor demasiado grande.");
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

            return errors;
        }

        /// <summary>
        /// Administrators only. Archiving is refused (<see cref="InUseException"/>) while an active test uses the design.
        /// </summary>
        public async Task SetArchivedAsync(ConcreteDesign design, bool archived, CancellationToken cancellationToken = default)
        {
            _session.RequireAdmin(AppSession.ArchiveAdminOnly);
            var blocker = archived ? await GetArchiveBlockerAsync(design, cancellationToken).ConfigureAwait(false) : null;
            if (blocker != null)
            {
                throw new InUseException(blocker);
            }

            await _repository.SetArchivedAsync(design, archived, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Why the design can't be archived (Spanish), or null when it can.</summary>
        public async Task<string> GetArchiveBlockerAsync(ConcreteDesign design, CancellationToken cancellationToken = default)
        {
            var usage = await _repository.GetUsageAsync(design.Id, cancellationToken).ConfigureAwait(false);
            return usage.IsInUse
                ? InUseException.BuildMessage($"la dosificación \"{design.Description}\"", usage, "ensayo activo", "ensayos activos",
                    "Archive primero esos ensayos.")
                : null;
        }
    }
}
