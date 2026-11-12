using System;

namespace ProbetaControl.Services
{
    /// <summary>The small helper calculators of the legacy forms.</summary>
    public static class Calculators
    {
        /// <summary>Design calculator: quantity of the target material as a percentage of the origin material.</summary>
        public static decimal ReplacementQuantity(decimal originQuantity, decimal percentage) =>
            originQuantity * percentage / 100m;

        /// <summary>
        /// Water/cement ratio (a/c) by weight, 2 decimals; null when there is no cement or no water
        /// (materials not typed yet).
        /// </summary>
        public static decimal? WaterCementRatio(decimal water, decimal cement) =>
            water > 0 && cement > 0 ? Math.Round(water / cement, 2, MidpointRounding.AwayFromZero) : (decimal?)null;

        /// <summary>Specimen form: average of the two measurements of diameter or height.</summary>
        public static decimal Average(decimal first, decimal second) => (first + second) / 2m;

        /// <summary>Specimen form: rupture date = mold date + test age in days.</summary>
        public static DateTime RuptureDate(DateTime moldDate, int days)
        {
            if (days < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(days));
            }

            return moldDate.Date.AddDays(days);
        }
    }
}
