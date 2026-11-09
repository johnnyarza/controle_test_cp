using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ProbetaControl.Models
{
    /// <summary>A concrete design (dosificación) with up to <see cref="MaxMaterials"/> material slots.</summary>
    public sealed class ConcreteDesign : IAuditable
    {
        public const int MaxMaterials = 8;

        public int Id { get; set; }

        public string Description { get; set; }

        /// <summary>Design strength, kg/cm².</summary>
        public decimal Fck { get; set; }

        /// <summary>cm.</summary>
        public decimal? Slump { get; set; }

        public bool IsArchived { get; set; }

        public int RowVersion { get; set; }

        /// <summary>Filled slots only, ordered by position.</summary>
        public List<ConcreteDesignMaterial> Materials { get; set; } = new List<ConcreteDesignMaterial>();

        public IReadOnlyDictionary<string, object> GetAuditValues()
        {
            var values = new Dictionary<string, object>
            {
                ["Description"] = Description,
                ["Fck"] = Fck,
                ["Slump"] = Slump,
            };

            for (var position = 1; position <= MaxMaterials; position++)
            {
                var slot = Materials.FirstOrDefault(m => m.Position == position && m.MaterialId.HasValue);
                values["Material" + position] = slot == null
                    ? null
                    : string.Format(CultureInfo.InvariantCulture, "{0}:{1}", slot.MaterialId, slot.QuantityKgPerM3);
            }

            return values;
        }

        public override string ToString() => Description;
    }

    public sealed class ConcreteDesignMaterial
    {
        /// <summary>1..8.</summary>
        public int Position { get; set; }

        public int? MaterialId { get; set; }

        public decimal? QuantityKgPerM3 { get; set; }

        /// <summary>Read-only, filled by queries.</summary>
        public string MaterialName { get; set; }

        /// <summary>Read-only, filled by queries.</summary>
        public string ProviderName { get; set; }
    }
}
