using System;
using System.Collections.Generic;
using ProbetaControl.Services;

namespace ProbetaControl.Models
{
    /// <summary>A concrete specimen (probeta). Age, density and strength are calculated, not stored.</summary>
    public class Specimen : IAuditable
    {
        public int Id { get; set; }

        public int CompressionTestId { get; set; }

        /// <summary>Free description (Descripción).</summary>
        public string Code { get; set; }

        /// <summary>cm.</summary>
        public decimal? Slump { get; set; }

        public DateTime MoldDate { get; set; }

        public DateTime RuptureDate { get; set; }

        public decimal DiameterCm { get; set; }

        public decimal HeightCm { get; set; }

        public decimal WeightKg { get; set; }

        /// <summary>Null until the specimen is tested.</summary>
        public decimal? RuptureLoadTon { get; set; }

        /// <summary>Measurements locked; set once the specimen is first saved (legacy rule).</summary>
        public bool IsLocked { get; set; }

        public bool IsArchived { get; set; }

        public int RowVersion { get; set; }

        public bool IsTested => RuptureLoadTon.HasValue && RuptureLoadTon.Value > 0;

        public int AgeDays => SpecimenMath.AgeDays(MoldDate, RuptureDate);

        public double? DensityKgPerM3 => SpecimenMath.DensityKgPerM3(DiameterCm, HeightCm, WeightKg);

        public double? StrengthKgPerCm2 => SpecimenMath.StrengthKgPerCm2(DiameterCm, RuptureLoadTon);

        public SpecimenStatus GetStatus(DateTime today) => SpecimenMath.Status(this, today);

        public IReadOnlyDictionary<string, object> GetAuditValues() => new Dictionary<string, object>
        {
            ["CompressionTestId"] = CompressionTestId,
            ["Code"] = Code,
            ["Slump"] = Slump,
            ["MoldDate"] = MoldDate.Date,
            ["RuptureDate"] = RuptureDate.Date,
            ["DiameterCm"] = DiameterCm,
            ["HeightCm"] = HeightCm,
            ["WeightKg"] = WeightKg,
            ["RuptureLoadTon"] = RuptureLoadTon,
            ["IsLocked"] = IsLocked,
        };

        public Specimen Clone() => (Specimen)MemberwiseClone();
    }

    /// <summary>A specimen due for testing, with the test it belongs to (warning list).</summary>
    public sealed class DueSpecimen : Specimen
    {
        public string ClientName { get; set; }

        public string ProjectName { get; set; }
    }

    /// <summary>Stored in English, shown in Spanish; also the grid row colour.</summary>
    public enum SpecimenStatus
    {
        /// <summary>Not tested, rupture date after tomorrow.</summary>
        Pending,

        /// <summary>Not tested, rupture date today or tomorrow (yellow).</summary>
        DueSoon,

        /// <summary>Not tested, rupture date already past (red).</summary>
        Overdue,

        /// <summary>Rupture load recorded.</summary>
        Tested,

        /// <summary>Archived (grey text).</summary>
        Archived,
    }
}
