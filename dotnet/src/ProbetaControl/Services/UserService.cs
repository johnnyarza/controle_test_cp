using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProbetaControl.Data;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    /// <summary>User administration. Every method requires an administrator.</summary>
    public sealed class UserService
    {
        private readonly UserRepository _users;
        private readonly AppSession _session;

        public UserService(UserRepository users, AppSession session)
        {
            _users = users;
            _session = session;
        }

        public Task<IReadOnlyList<User>> ListAsync(bool includeArchived, CancellationToken cancellationToken = default)
        {
            _session.RequireAdmin();
            return _users.ListAsync(includeArchived, cancellationToken);
        }

        /// <summary>Creates a user who must choose a new password at first login.</summary>
        public async Task CreateAsync(User user, string initialPassword, CancellationToken cancellationToken = default)
        {
            _session.RequireAdmin();
            Validate(user);
            AuthService.ValidateNewPassword(initialPassword, "Password");

            user.PasswordHash = await AuthService.HashAsync(initialPassword).ConfigureAwait(false);
            user.MustChangePassword = true;
            await _users.InsertAsync(user, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _session.RequireAdmin();
            Validate(user);

            var stored = await _users.GetAsync(user.Id, cancellationToken).ConfigureAwait(false) ?? throw new NotFoundException("el usuario");
            if (stored.IsAdmin && !user.IsAdmin)
            {
                await EnsureAnotherAdminRemainsAsync(stored, cancellationToken).ConfigureAwait(false);
            }

            await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Sets a temporary password the user must change at next login.</summary>
        public async Task ResetPasswordAsync(int userId, string temporaryPassword, CancellationToken cancellationToken = default)
        {
            _session.RequireAdmin();
            AuthService.ValidateNewPassword(temporaryPassword, "Password");
            var hash = await AuthService.HashAsync(temporaryPassword).ConfigureAwait(false);
            await _users.SetPasswordAsync(userId, hash, true, cancellationToken).ConfigureAwait(false);
        }

        public async Task SetArchivedAsync(User user, bool archived, CancellationToken cancellationToken = default)
        {
            _session.RequireAdmin();
            if (archived)
            {
                if (user.Id == _session.UserId)
                {
                    throw new AccessDeniedException("No puede archivar su propio usuario.");
                }

                if (user.IsAdmin)
                {
                    await EnsureAnotherAdminRemainsAsync(user, cancellationToken).ConfigureAwait(false);
                }
            }

            await _users.SetArchivedAsync(user, archived, cancellationToken).ConfigureAwait(false);
        }

        private async Task EnsureAnotherAdminRemainsAsync(User user, CancellationToken cancellationToken)
        {
            if (!user.IsArchived && await _users.CountActiveAdminsAsync(cancellationToken).ConfigureAwait(false) <= 1)
            {
                throw new AccessDeniedException("Debe quedar al menos un administrador activo.");
            }
        }

        private static void Validate(User user)
        {
            var errors = new ValidationErrors()
                .Required("UserName", user.UserName)
                .MaxLength("UserName", user.UserName, 50)
                .MaxLength("FullName", user.FullName, 100);

            if (!string.IsNullOrWhiteSpace(user.UserName) && user.UserName.Trim().Any(char.IsWhiteSpace))
            {
                errors.Add("UserName", "El nombre de usuario no puede tener espacios.");
            }

            if (!UserRoles.All.Contains(user.Role))
            {
                errors.Add("Role", "Seleccione un rol.");
            }

            errors.ThrowIfAny();
        }
    }
}
