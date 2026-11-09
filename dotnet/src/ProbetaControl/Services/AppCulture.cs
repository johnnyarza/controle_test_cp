using System;
using System.Globalization;
using System.Threading;

namespace ProbetaControl.Services
{
    /// <summary>
    /// The app never depends on the PC's regional settings (the dev PC is en-CA, office PCs vary).
    /// Everything is formatted with the invariant culture; dates are shown as dd/MM/yyyy.
    /// </summary>
    public static class AppCulture
    {
        public const string DatePattern = "dd/MM/yyyy";

        public static CultureInfo Culture { get; } = Create();

        /// <summary>Makes the invariant-based culture the default for the UI thread and every worker thread.</summary>
        public static void Apply()
        {
            CultureInfo.DefaultThreadCurrentCulture = Culture;
            Thread.CurrentThread.CurrentCulture = Culture;
        }

        public static string FormatDate(DateTime? value) =>
            value?.ToString(DatePattern, CultureInfo.InvariantCulture) ?? string.Empty;

        private static CultureInfo Create()
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.ShortDatePattern = DatePattern;
            culture.DateTimeFormat.LongDatePattern = DatePattern;
            culture.DateTimeFormat.DateSeparator = "/";
            return CultureInfo.ReadOnly(culture);
        }
    }
}
