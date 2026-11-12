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

        /// <summary>One line for lists: "Cemento 350 · Arena 800 · Grava 1000" (kg/m³).</summary>
        public string MaterialsSummary => string.Join(" · ", Materials
            .Where(m => m.MaterialId.HasValue)
            .OrderBy(m => m.Position)
            .Select(m => string.Format(CultureInfo.InvariantCulture, "{0} {1:0.###}", m.MaterialName, m.QuantityKgPerM3)));

        /// <summary>Water / cement by weight (materials typed Agua and Cemento); null when either is missing.</summary>
        public decimal? WaterCementRatio => Services.Calculators.WaterCementRatio(
            Materials.Where(m => m.MaterialId.HasValue && m.MaterialType == MaterialTypes.Water).Sum(m => m.QuantityKgPerM3 ?? 0),
            Materials.Where(m => m.MaterialId.HasValue && m.MaterialType == MaterialTypes.Cement).Sum(m => m.QuantityKgPerM3 ?? 0));

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

        /// <summary>Read-only, filled by queries (<see cref="MaterialTypes"/>).</summary>
        public string MaterialType { get; set; }
    }
}
