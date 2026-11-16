using System;
using ProbetaControl.Services;

namespace ProbetaControl.ViewModels.Infrastructure
{
    /// <summary>
    /// The note above "Mostrar más": the legacy program showed every row at once, so users don't expect lists to load
    /// 100 rows at a time. Shown once a day on this PC (any list), the first time a list has rows not loaded yet.
    /// </summary>
    public static class MoreRowsHint
    {
        /// <summary>Replaceable by tests.</summary>
        internal static Func<DateTime> Today { get; set; } = () => DateTime.Today;

        /// <summary>True (and remembered for today) when the note hasn't been shown today.</summary>
        public static bool TryShowToday()
        {
            var today = Today().Date;
            if (LocalSettings.MoreRowsHintShownOn == today)
            {
                return false;
            }

            LocalSettings.MoreRowsHintShownOn = today;
            return true;
        }
    }
}
