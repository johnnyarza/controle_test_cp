using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ProbetaControl.Models
{
    /// <summary>The "Estado" choice of the probetas filter.</summary>
    public enum SpecimenStateFilter
    {
        All,

        /// <summary>No rupture load yet ("Sin romper").</summary>
        Untested,

        /// <summary>Not broken and the rupture date is today or earlier ("Para romper", like the page's tile).</summary>
        Due,

        /// <summary>Rupture load recorded ("Rotas").</summary>
        Tested,
    }

    /// <summary>Which date the "Entre fechas" range of the probetas filter looks at.</summary>
    public enum SpecimenDateField
    {
        Mold,
        Rupture,
    }

    /// <summary>
    /// The probetas filter of one ensayo (the panel under "Filtrar"). Every part that is set must match; an empty
    /// filter keeps every probeta. A test holds tens of probetas, so <see cref="Matches"/> runs in memory and is the one
    /// rule for the page, its counts and what Imprimir, Excel and Etiquetas print.
    /// </summary>
    public sealed class SpecimenFilter
    {
        public static SpecimenFilter None { get; } = new SpecimenFilter();

        public SpecimenStateFilter State { get; set; }

        /// <summary>Mold days picked from the ensayo's own days (any of them).</summary>
        public IReadOnlyList<DateTime> MoldDays { get; set; } = Array.Empty<DateTime>();

        /// <summary>The date <see cref="DateFrom"/> / <see cref="DateTo"/> apply to.</summary>
        public SpecimenDateField DateField { get; set; }

        /// <summary>Inclusive; null = open.</summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>Inclusive; null = open.</summary>
        public DateTime? DateTo { get; set; }

        /// <summary>Ages in days (any of them).</summary>
        public IReadOnlyList<int> Ages { get; set; } = Array.Empty<int>();

        /// <summary>Inclusive ID ranges, e.g. from "1-5, 8".</summary>
        public IReadOnlyList<IdRange> IdRanges { get; set; } = Array.Empty<IdRange>();

        /// <summary>Part of the description (Code), ignoring case and accents.</summary>
        public string CodeText { get; set; }

        /// <summary>Archived probetas too (shown in grey; never printed).</summary>
        public bool IncludeArchived { get; set; }

        /// <summary>True when something besides <see cref="IncludeArchived"/> narrows the list.</summary>
        public bool HasCriteria =>
            State != SpecimenStateFilter.All || MoldDays.Count > 0 || DateFrom.HasValue || DateTo.HasValue || Ages.Count > 0 ||
            IdRanges.Count > 0 || !string.IsNullOrWhiteSpace(CodeText);

        public bool Matches(Specimen specimen, DateTime today)
        {
            if (specimen.IsArchived && !IncludeArchived)
            {
                return false;
            }

            switch (State)
            {
                case SpecimenStateFilter.Untested when specimen.IsTested:
                case SpecimenStateFilter.Tested when !specimen.IsTested:
                case SpecimenStateFilter.Due when specimen.IsTested || specimen.RuptureDate.Date > today.Date:
                    return false;
            }

            if (MoldDays.Count > 0 && !MoldDays.Any(d => d.Date == specimen.MoldDate.Date))
            {
                return false;
            }

            var date = (DateField == SpecimenDateField.Rupture ? specimen.RuptureDate : specimen.MoldDate).Date;
            if ((DateFrom.HasValue && date < DateFrom.Value.Date) || (DateTo.HasValue && date > DateTo.Value.Date))
            {
                return false;
            }

            if (Ages.Count > 0 && !Ages.Contains(specimen.AgeDays))
            {
                return false;
            }

            if (IdRanges.Count > 0 && !IdRanges.Any(r => specimen.Id >= r.From && specimen.Id <= r.To))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(CodeText) || ContainsText(specimen.Code, CodeText.Trim());
        }

        /// <summary>A copy with other <see cref="IncludeArchived"/> (printouts never include archived probetas).</summary>
        public SpecimenFilter WithArchived(bool includeArchived)
        {
            var copy = (SpecimenFilter)MemberwiseClone();
            copy.IncludeArchived = includeArchived;
            return copy;
        }

        public SpecimenFilter Clone() => (SpecimenFilter)MemberwiseClone();

        /// <summary>"Columna" finds "columna C-3" and "Columna"; "losa" finds "Losa"; accents don't matter.</summary>
        public static bool ContainsText(string text, string part) =>
            !string.IsNullOrEmpty(text) &&
            CultureInfo.InvariantCulture.CompareInfo.IndexOf(text, part, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    }
}
