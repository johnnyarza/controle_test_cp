using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProbetaControl.Data;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    public sealed class CompressionTestService
    {
        private readonly CompressionTestRepository _repository;
        private readonly IClock _clock;

        public CompressionTestService(CompressionTestRepository repository, IClock clock)
        {
            _repository = repository;
            _clock = clock;
        }

        public Task<PagedResult<CompressionTestListItem>> SearchAsync(CompressionTestQuery query, CancellationToken cancellationToken = default) =>
            _repository.SearchAsync(query, _clock.Today, cancellationToken);

        public Task<CompressionTest> GetAsync(int id, CancellationToken cancellationToken = default) => _repository.GetAsync(id, cancellationToken);

        public DateTime Today => _clock.Today;

        /// <summary>Active tests with specimens to break today or earlier.</summary>
        public Task<int> CountWithDueAsync(CancellationToken cancellationToken = default) =>
            _repository.CountWithDueAsync(_clock.Today, cancellationToken);

        /// <param name="testUnlocked">Admin, unlocked by an admin, or a test created in this session.</param>
        public Task SaveAsync(CompressionTest test, bool testUnlocked, CancellationToken cancellationToken = default)
        {
            if (test.Id != 0)
            {
                Require(SpecimenEditPolicy.CanChangeStructure(testUnlocked));
            }

            Validate(test).ThrowIfAny();
            return test.Id == 0 ? _repository.InsertAsync(test, cancellationToken) : _repository.UpdateAsync(test, cancellationToken);
        }

        /// <summary>Header rules. Keys: ProjectName, Address, ClientId, ConcreteProviderId, ConcreteDesignId, TestDate.</summary>
        public static ValidationErrors Validate(CompressionTest test, ValidationErrors errors = null)
        {
            errors = (errors ?? new ValidationErrors())
                .Required("ProjectName", test.ProjectName)
                .MaxLength("ProjectName", test.ProjectName, 200)
                .Required("Address", test.Address)
                .MaxLength("Address", test.Address, 200);

            if (test.ClientId <= 0)
            {
                errors.Add("ClientId", "Seleccione el cliente.");
            }

            if (test.ConcreteProviderId <= 0)
            {
                errors.Add("ConcreteProviderId", "Seleccione el proveedor del hormigón.");
            }

            if (test.ConcreteDesignId <= 0)
            {
                errors.Add("ConcreteDesignId", "Seleccione la dosificación.");
            }

            if (test.TestDate == default)
            {
                errors.Add("TestDate", "Ingrese la fecha.");
            }

            return errors;
        }

        public Task SetArchivedAsync(CompressionTest test, bool archived, bool testUnlocked, CancellationToken cancellationToken = default)
        {
            Require(SpecimenEditPolicy.CanChangeStructure(testUnlocked));
            return _repository.SetArchivedAsync(test, archived, cancellationToken);
        }

        internal static void Require(EditDecision decision)
        {
            if (!decision.IsAllowed)
            {
                throw new AccessDeniedException(decision.Reason);
            }
        }
    }

    public sealed class SpecimenService
    {
        private readonly SpecimenRepository _repository;
        private readonly IClock _clock;

        public SpecimenService(SpecimenRepository repository, IClock clock)
        {
            _repository = repository;
            _clock = clock;
        }

        public DateTime Today => _clock.Today;

        public Task<Specimen> GetAsync(int id, CancellationToken cancellationToken = default) => _repository.GetAsync(id, cancellationToken);

        public Task<IReadOnlyList<Specimen>> ListByTestAsync(int compressionTestId, SpecimenFilter filter = null,
            CancellationToken cancellationToken = default) =>
            _repository.ListByTestAsync(compressionTestId, filter, cancellationToken);

        public Task<int> CountDueAsync(int compressionTestId, CancellationToken cancellationToken = default) =>
            _repository.CountDueAsync(compressionTestId, _clock.Today, cancellationToken);

        /// <summary>Warning list: untested specimens due today or tomorrow, plus all overdue ones.</summary>
        public Task<IReadOnlyList<DueSpecimen>> ListDueAsync(CancellationToken cancellationToken = default) =>
            _repository.ListDueAsync(_clock.Today.AddDays(1), cancellationToken);

        public EditDecision CanEdit(Specimen specimen, bool testUnlocked) =>
            SpecimenEditPolicy.CanEdit(specimen, testUnlocked, _clock.Today);

        /// <summary>
        /// Validates and saves. A new specimen needs an unlocked test; an existing one follows
        /// <see cref="SpecimenEditPolicy"/> (operators may only record the rupture load).
        /// Saving locks the measurements, as in the legacy app.
        /// </summary>
        public async Task SaveAsync(Specimen specimen, bool testUnlocked, CancellationToken cancellationToken = default)
        {
            Validate(specimen);

            if (specimen.Id == 0)
            {
                CompressionTestService.Require(SpecimenEditPolicy.CanChangeStructure(testUnlocked));
                specimen.IsLocked = true;
                await _repository.InsertAsync(specimen, cancellationToken).ConfigureAwait(false);
                return;
            }

            var stored = await _repository.GetAsync(specimen.Id, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException("la probeta");
            var decision = SpecimenEditPolicy.CanEdit(stored, testUnlocked, _clock.Today);
            CompressionTestService.Require(decision);

            if (decision.IsRuptureLoadOnly && !OnlyRuptureLoadChanged(stored, specimen))
            {
                throw new AccessDeniedException("Solo se puede registrar la rotura. El administrador debe desbloquear el ensayo para cambiar las medidas.");
            }

            specimen.IsLocked = true;
            await _repository.UpdateAsync(specimen, cancellationToken).ConfigureAwait(false);
        }

        public Task SetArchivedAsync(Specimen specimen, bool archived, bool testUnlocked, CancellationToken cancellationToken = default)
        {
            CompressionTestService.Require(SpecimenEditPolicy.CanChangeStructure(testUnlocked));
            return _repository.SetArchivedAsync(specimen, archived, cancellationToken);
        }

        private static bool OnlyRuptureLoadChanged(Specimen stored, Specimen edited) =>
            stored.CompressionTestId == edited.CompressionTestId
            && (stored.Code ?? string.Empty) == (edited.Code?.Trim() ?? string.Empty)
            && stored.Slump == edited.Slump
            && stored.MoldDate.Date == edited.MoldDate.Date
            && stored.RuptureDate.Date == edited.RuptureDate.Date
            && stored.DiameterCm == edited.DiameterCm
            && stored.HeightCm == edited.HeightCm
            && stored.WeightKg == edited.WeightKg;

        private static void Validate(Specimen specimen)
        {
            var errors = new ValidationErrors().MaxLength("Code", specimen.Code, 100);

            if (specimen.CompressionTestId <= 0)
            {
                errors.Add("CompressionTestId", "La probeta debe pertenecer a un ensayo.");
            }

            if (specimen.MoldDate == default)
            {
                errors.Add("MoldDate", "Ingrese la fecha de moldeo.");
            }

            if (specimen.RuptureDate == default)
            {
                errors.Add("RuptureDate", "Ingrese la fecha de rotura.");
            }
            else if (specimen.MoldDate != default && specimen.RuptureDate.Date < specimen.MoldDate.Date)
            {
                errors.Add("RuptureDate", "La fecha de rotura no puede ser anterior a la de moldeo.");
            }

            if (specimen.DiameterCm <= 0)
            {
                errors.Add("DiameterCm", "Ingrese el diámetro.");
            }

            if (specimen.HeightCm <= 0)
            {
                errors.Add("HeightCm", "Ingrese la altura.");
            }

            if (specimen.WeightKg <= 0)
            {
                errors.Add("WeightKg", "Ingrese el peso.");
            }

            if (specimen.Slump < 0)
            {
                errors.Add("Slump", "El slump no puede ser negativo.");
            }

            if (specimen.RuptureLoadTon < 0)
            {
                errors.Add("RuptureLoadTon", "La rotura no puede ser negativa.");
            }

            errors.ThrowIfAny();

            // Legacy stored 0 for "not tested"; the new schema uses NULL.
            if (specimen.RuptureLoadTon == 0)
            {
                specimen.RuptureLoadTon = null;
            }
        }
    }

    /// <summary>Report logo and stamp, shared by all PCs. Changing them requires an administrator.</summary>
    public sealed class ReportImageService
    {
        public const int MaxBytes = 2 * 1024 * 1024;

        private readonly AppSettingsRepository _repository;
        private readonly AppSession _session;

        public ReportImageService(AppSettingsRepository repository, AppSession session)
        {
            _repository = repository;
            _session = session;
        }

        public Task<StoredImage> GetLogoAsync(CancellationToken cancellationToken = default) =>
            _repository.GetImageAsync(AppSettingsRepository.ReportLogo, cancellationToken);

        public Task<StoredImage> GetStampAsync(CancellationToken cancellationToken = default) =>
            _repository.GetImageAsync(AppSettingsRepository.ReportStamp, cancellationToken);

        public Task SetLogoAsync(byte[] bytes, string contentType, CancellationToken cancellationToken = default) =>
            SetAsync(AppSettingsRepository.ReportLogo, bytes, contentType, cancellationToken);

        public Task SetStampAsync(byte[] bytes, string contentType, CancellationToken cancellationToken = default) =>
            SetAsync(AppSettingsRepository.ReportStamp, bytes, contentType, cancellationToken);

        private Task SetAsync(string key, byte[] bytes, string contentType, CancellationToken cancellationToken)
        {
            _session.RequireAdmin();
            if (bytes != null)
            {
                if (bytes.Length > MaxBytes)
                {
                    throw new ValidationException("Image", "La imagen es demasiado grande (máximo 2 MB).");
                }

                if (contentType != "image/png" && contentType != "image/jpeg")
                {
                    throw new ValidationException("Image", "Use una imagen PNG o JPG.");
                }
            }

            return _repository.SetImageAsync(key, bytes, contentType, cancellationToken);
        }
    }
}
