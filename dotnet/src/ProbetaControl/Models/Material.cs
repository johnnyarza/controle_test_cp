using System.Collections.Generic;

namespace ProbetaControl.Models
{
    public sealed class Material : IAuditable
    {
        public int Id { get; set; }

        public string Name { get; set; }

        /// <summary>One of <see cref="MaterialTypes"/>.</summary>
        public string MaterialType { get; set; } = MaterialTypes.Other;

        public string TypeLabel => MaterialTypes.Label(MaterialType);

        public int? ProviderId { get; set; }

        /// <summary>Read-only, filled by queries.</summary>
        public string ProviderName { get; set; }

        public bool IsArchived { get; set; }

        public int RowVersion { get; set; }

        /// <summary>"Arena fina – Arenera Sur": the same material name can come from two providers.</summary>
        public string DisplayName => string.IsNullOrEmpty(ProviderName) ? Name : Name + " – " + ProviderName;

        public IReadOnlyDictionary<string, object> GetAuditValues() => new Dictionary<string, object>
        {
            ["Name"] = Name,
            ["MaterialType"] = MaterialType,
            ["ProviderId"] = ProviderId,
        };

        public override string ToString() => Name;
    }
}
