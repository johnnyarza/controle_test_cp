using System.Collections.Generic;
using System.Linq;

namespace ProbetaControl.Models
{
    /// <summary>
    /// What a material is in a mix. Stored in English (Materials.MaterialType), shown in Spanish.
    /// Cement and water give the design's water/cement ratio.
    /// </summary>
    public static class MaterialTypes
    {
        public const string Cement = "cement";
        public const string Water = "water";
        public const string Aggregate = "aggregate";
        public const string Admixture = "admixture";
        public const string Other = "other";

        /// <summary>In the order the form lists them.</summary>
        public static IReadOnlyList<MaterialTypeOption> All { get; } = new[]
        {
            new MaterialTypeOption(Cement, "Cemento"),
            new MaterialTypeOption(Water, "Agua"),
            new MaterialTypeOption(Aggregate, "Agregado"),
            new MaterialTypeOption(Admixture, "Aditivo"),
            new MaterialTypeOption(Other, "Otro"),
        };

        public static bool IsValid(string type) => All.Any(o => o.Value == type);

        public static string Label(string type) => All.FirstOrDefault(o => o.Value == type)?.Label ?? "Otro";
    }

    public sealed class MaterialTypeOption
    {
        public MaterialTypeOption(string value, string label)
        {
            Value = value;
            Label = label;
        }

        public string Value { get; }

        public string Label { get; }

        public override string ToString() => Label;
    }
}
