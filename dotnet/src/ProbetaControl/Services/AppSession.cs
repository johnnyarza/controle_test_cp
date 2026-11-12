using System;
using System.Collections.Generic;
using ProbetaControl.Data;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    /// <summary>The signed-in user on this PC. Also tells the audit log who made each change.</summary>
    public sealed class AppSession : IAuditContext
    {
        public AppSession(string machineName = null)
        {
            MachineName = machineName ?? Environment.MachineName;
        }

        public User CurrentUser { get; private set; }

        public int? UserId => CurrentUser?.Id;

        public string MachineName { get; }

        public bool IsSignedIn => CurrentUser != null;

        public bool IsAdmin => CurrentUser?.IsAdmin == true;

        public void SignIn(User user)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
            _unlockedTests.Clear();
        }

        public void SignOut()
        {
            CurrentUser = null;
            _unlockedTests.Clear();
        }

        private readonly HashSet<int> _unlockedTests = new HashSet<int>();

        /// <summary>
        /// The user may change the test's header and specimens (see <see cref="SpecimenEditPolicy"/>):
        /// always for an administrator; for an operator only for tests they created in this session
        /// or an administrator unlocked for them. Ends with the session.
        /// </summary>
        public bool IsTestUnlocked(int testId) => IsAdmin || _unlockedTests.Contains(testId);

        /// <summary>A test created by this user, or unlocked for them by an administrator.</summary>
        public void UnlockTest(int testId) => _unlockedTests.Add(testId);

        /// <summary>"Bloquear": the operator gives the unlock back before leaving the PC.</summary>
        public void LockTest(int testId) => _unlockedTests.Remove(testId);

        /// <summary>Archiving and restoring any record is for administrators (checked in every service).</summary>
        public const string ArchiveAdminOnly = "Solo un administrador puede archivar o restaurar.";

        public void RequireAdmin(string message = null)
        {
            if (!IsAdmin)
            {
                throw new AccessDeniedException(message ?? "Esta acción requiere un usuario administrador.");
            }
        }
    }
}
