using System.Collections.Generic;

namespace ProbetaControl.Models
{
    /// <summary>A record whose stored fields are written to AuditLog when it changes.</summary>
    public interface IAuditable
    {
        /// <summary>Stored fields by column name. Never include secrets (password hashes).</summary>
        IReadOnlyDictionary<string, object> GetAuditValues();
    }
}
