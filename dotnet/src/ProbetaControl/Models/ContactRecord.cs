using System.Collections.Generic;

namespace ProbetaControl.Models
{
    /// <summary>Shared shape of clients and material providers.</summary>
    public abstract class ContactRecord : IAuditable
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public string Email { get; set; }

        public bool IsArchived { get; set; }

        public int RowVersion { get; set; }

        public IReadOnlyDictionary<string, object> GetAuditValues() => new Dictionary<string, object>
        {
            ["Name"] = Name,
            ["Phone"] = Phone,
            ["Address"] = Address,
            ["Email"] = Email,
        };

        public override string ToString() => Name;
    }

    public sealed class Client : ContactRecord
    {
    }

    public sealed class Provider : ContactRecord
    {
    }
}
