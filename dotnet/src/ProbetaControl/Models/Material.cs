using System.Collections.Generic;

namespace ProbetaControl.Models
{
    public sealed class Material : IAuditable
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? ProviderId { get; set; }

        /// <summary>Read-only, filled by queries.</summary>
        public string ProviderName { get; set; }

        public bool IsArchived { get; set; }

        public int RowVersion { get; set; }

        public IReadOnlyDictionary<string, object> GetAuditValues() => new Dictionary<string, object>
        {
            ["Name"] = Name,
            ["ProviderId"] = ProviderId,
        };

        public override string ToString() => Name;
    }
}
