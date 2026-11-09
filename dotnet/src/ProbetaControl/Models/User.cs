using System.Collections.Generic;

namespace ProbetaControl.Models
{
    public sealed class User : IAuditable
    {
        public int Id { get; set; }

        public string UserName { get; set; }

        public string FullName { get; set; }

        /// <summary>BCrypt hash. Never shown, never audited.</summary>
        public string PasswordHash { get; set; }

        /// <summary><see cref="UserRoles.Admin"/> or <see cref="UserRoles.Operator"/>.</summary>
        public string Role { get; set; }

        public bool MustChangePassword { get; set; }

        public bool IsArchived { get; set; }

        public int RowVersion { get; set; }

        public bool IsAdmin => Role == UserRoles.Admin;

        public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? UserName : FullName;

        public IReadOnlyDictionary<string, object> GetAuditValues() => new Dictionary<string, object>
        {
            ["UserName"] = UserName,
            ["FullName"] = FullName,
            ["Role"] = Role,
            ["MustChangePassword"] = MustChangePassword,
        };
    }

    /// <summary>Role values as stored in the database.</summary>
    public static class UserRoles
    {
        public const string Admin = "admin";
        public const string Operator = "operator";

        public static readonly IReadOnlyList<string> All = new[] { Admin, Operator };
    }
}
