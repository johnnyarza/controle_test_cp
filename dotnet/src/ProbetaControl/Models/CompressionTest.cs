using System;
using System.Collections.Generic;

namespace ProbetaControl.Models
{
    /// <summary>A compression test (ensayo): one client project and pour, holding many specimens.</summary>
    public sealed class CompressionTest : IAuditable
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public int ConcreteProviderId { get; set; }

        public int ConcreteDesignId { get; set; }

        /// <summary>Obra / proyecto.</summary>
        public string ProjectName { get; set; }

        public string Address { get; set; }

        public DateTime TestDate { get; set; }

        public bool IsArchived { get; set; }

        public int RowVersion { get; set; }

        /// <summary>Read-only, filled by queries.</summary>
        public string ClientName { get; set; }

        /// <summary>Read-only, filled by queries.</summary>
        public string ConcreteProviderName { get; set; }

        /// <summary>Read-only, filled by queries.</summary>
        public string ConcreteDesignDescription { get; set; }

        /// <summary>Read-only, filled by queries: the design strength (kg/cm²) results are compared with.</summary>
        public decimal ConcreteDesignFck { get; set; }

        public IReadOnlyDictionary<string, object> GetAuditValues() => new Dictionary<string, object>
        {
            ["ClientId"] = ClientId,
            ["ConcreteProviderId"] = ConcreteProviderId,
            ["ConcreteDesignId"] = ConcreteDesignId,
            ["ProjectName"] = ProjectName,
            ["Address"] = Address,
            ["TestDate"] = TestDate.Date,
        };
    }

    /// <summary>One row of the test list.</summary>
    public sealed class CompressionTestListItem
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public string ClientName { get; set; }

        public string ProjectName { get; set; }

        public string Address { get; set; }

        public DateTime TestDate { get; set; }

        public int SpecimenCount { get; set; }

        /// <summary>Untested specimens whose rupture date is today or earlier.</summary>
        public int DueCount { get; set; }

        public bool IsArchived { get; set; }
    }
}
