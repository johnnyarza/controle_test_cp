using System.Threading;
using System.Threading.Tasks;
using ProbetaControl.Data;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    /// <summary>Login, password changes and the administrator check used to unlock a test.</summary>
    public sealed class AuthService
    {
        public const int MinPasswordLength = 8;
        private const int WorkFactor = 11;
        private const string WrongCredentials = "Usuario o contraseña incorrectos.";

        private static readonly System.Lazy<string> DummyHash =
            new System.Lazy<string>(() => BCrypt.Net.BCrypt.HashPassword(System.Guid.NewGuid().ToString(), WorkFactor));

        private readonly UserRepository _users;
        private readonly AppSession _session;

        public AuthService(UserRepository users, AppSession session)
        {
            _users = users;
            _session = session;
        }

        /// <summary>BCrypt is deliberately slow (~0.2 s), so it runs off the UI thread.</summary>
        public static Task<string> HashAsync(string password) =>
            Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor));

        public static Task<bool> VerifyAsync(string password, string hash) =>
            Task.Run(() => !string.IsNullOrEmpty(hash) && BCrypt.Net.BCrypt.Verify(password ?? string.Empty, hash));

        /// <summary>Checks the credentials and, if they are right, signs the user in.</summary>
        public async Task<User> LoginAsync(string userName, string password, CancellationToken cancellationToken = default)
        {
            var user = await CheckCredentialsAsync(userName, password, cancellationToken).ConfigureAwait(false);
            _session.SignIn(user);
            AppLog.Info($"Login {user.UserName} on {_session.MachineName}");
            return user;
        }

        /// <summary>For "Desbloquear": an administrator types their credentials on someone else's session.</summary>
        public async Task<User> VerifyAdministratorAsync(string userName, string password, CancellationToken cancellationToken = default)
        {
            var user = await CheckCredentialsAsync(userName, password, cancellationToken).ConfigureAwait(false);
            if (!user.IsAdmin)
            {
                throw new AccessDeniedException("El usuario indicado no es administrador.");
            }

            return user;
        }

        /// <summary>The signed-in user changes their own password.</summary>
        public async Task ChangeOwnPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = _session.CurrentUser ?? throw new AccessDeniedException("No hay una sesión iniciada.");
            ValidateNewPassword(newPassword, "NewPassword");

            var stored = await _users.GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
            if (stored == null || !await VerifyAsync(currentPassword, stored.PasswordHash).ConfigureAwait(false))
            {
                throw new ValidationException("CurrentPassword", "La contraseña actual no es correcta.");
            }

            if (currentPassword == newPassword)
            {
                throw new ValidationException("NewPassword", "La nueva contraseña debe ser distinta de la actual.");
            }

            var hash = await HashAsync(newPassword).ConfigureAwait(false);
            await _users.SetPasswordAsync(user.Id, hash, false, cancellationToken).ConfigureAwait(false);
            user.MustChangePassword = false;
        }

        public static void ValidateNewPassword(string password, string field)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            {
                throw new ValidationException(field, $"La contraseña debe tener al menos {MinPasswordLength} caracteres.");
            }
        }

        private async Task<User> CheckCredentialsAsync(string userName, string password, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
            {
                throw new ValidationException("UserName", "Ingrese usuario y contraseña.");
            }

            var user = await _users.GetByUserNameAsync(userName, cancellationToken).ConfigureAwait(false);

            // Same message and the same BCrypt cost whether or not the user exists.
            var hash = user?.PasswordHash ?? await Task.Run(() => DummyHash.Value).ConfigureAwait(false);
            var ok = await VerifyAsync(password, hash).ConfigureAwait(false);
            if (user == null || !ok || user.IsArchived)
            {
                throw new ValidationException("UserName", WrongCredentials);
            }

            return user;
        }
    }
}
