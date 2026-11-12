using System;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    /// <summary>Specimen calculations, identical to the legacy Java app (CorpoDeProva.java).</summary>
    public static class SpecimenMath
    {
        /// <summary>Whole days between molding and rupture.</summary>
        public static int AgeDays(DateTime moldDate, DateTime ruptureDate) =>
            Math.Abs((ruptureDate.Date - moldDate.Date).Days);

        /// <summary>
        /// kg/m³ = weight / (π·d²/4 [cm²→m²] · h [cm→m]). Null when diameter or height is zero.
        /// </summary>
        public static double? DensityKgPerM3(decimal diameterCm, decimal heightCm, decimal weightKg)
        {
            if (diameterCm <= 0 || heightCm <= 0)
            {
                return null;
            }

            var d = (double)diameterCm;
            var areaM2 = Math.PI * d * d / 4 / 10000;
            var volumeM3 = areaM2 * ((double)heightCm / 100);
            return (double)weightKg / volumeM3;
        }

        /// <summary>
        /// kg/cm² = load [t → kg] / (π·d²/4). Null until a rupture load is recorded.
        /// </summary>
        public static double? StrengthKgPerCm2(decimal diameterCm, decimal? ruptureLoadTon)
        {
            if (diameterCm <= 0 || !ruptureLoadTon.HasValue || ruptureLoadTon.Value <= 0)
            {
                return null;
            }

            var d = (double)diameterCm;
            var areaCm2 = Math.PI * d * d / 4;
            return (double)ruptureLoadTon.Value * 1000 / areaCm2;
        }

        /// <summary>Strength as a percentage of the design fck (100 = reached it). Null without a result or an fck.</summary>
        public static double? PercentOfFck(double? strengthKgPerCm2, decimal designFck) =>
            strengthKgPerCm2.HasValue && designFck > 0 ? strengthKgPerCm2.Value / (double)designFck * 100 : (double?)null;

        public static SpecimenStatus Status(Specimen specimen, DateTime today)
        {
            if (specimen.IsArchived)
            {
                return SpecimenStatus.Archived;
            }

            if (specimen.IsTested)
            {
                return SpecimenStatus.Tested;
            }

            var rupture = specimen.RuptureDate.Date;
            if (rupture < today.Date)
            {
                return SpecimenStatus.Overdue;
            }

            return rupture <= today.Date.AddDays(1) ? SpecimenStatus.DueSoon : SpecimenStatus.Pending;
        }
    }
}
