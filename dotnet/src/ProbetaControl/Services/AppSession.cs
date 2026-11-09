using System;
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

        public void SignIn(User user) => CurrentUser = user ?? throw new ArgumentNullException(nameof(user));

        public void SignOut() => CurrentUser = null;

        public void RequireAdmin()
        {
            if (!IsAdmin)
            {
                throw new AccessDeniedException("Esta acción requiere un usuario administrador.");
            }
        }
    }
}
