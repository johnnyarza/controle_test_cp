using System;
using System.Globalization;
using System.Threading;

namespace ProbetaControl.Services
{
    /// <summary>
    /// The app's working culture is invariant (the dev PC is en-CA, office PCs vary), so parsing, storage and
    /// file names never depend on the PC; dates are always dd/MM/yyyy. Numbers shown to people (lists, tiles,
    /// reports) use <see cref="Display"/>: the PC's decimal and thousands separators, read at startup.
    /// IDs are never grouped: format them with <see cref="CultureInfo.InvariantCulture"/> and no pattern.
    /// </summary>
    public static class AppCulture
    {
        public const string DatePattern = "dd/MM/yyyy";

        // A static constructor so the PC's culture is read before Apply() replaces the thread culture.
        static AppCulture()
        {
            Display = CreateDisplay(CultureInfo.CurrentCulture.NumberFormat);
            Culture = CreateWorking();
        }

        /// <summary>The working culture of every thread (invariant, dates dd/MM/yyyy).</summary>
        public static CultureInfo Culture { get; }

        /// <summary>For numbers shown to people: invariant, with the PC's number separators.</summary>
        public static CultureInfo Display { get; }

        /// <summary>Makes the invariant-based culture the default for the UI thread and every worker thread.</summary>
        public static void Apply()
        {
            CultureInfo.DefaultThreadCurrentCulture = Culture;
            Thread.CurrentThread.CurrentCulture = Culture;
        }

        public static string FormatDate(DateTime? value) =>
            value?.ToString(DatePattern, CultureInfo.InvariantCulture) ?? string.Empty;

        /// <summary>
        /// A grouped number pattern: <c>Pattern(2)</c> = "#,##0.00" (always two decimals),
        /// <c>Pattern(0, 2)</c> = "#,##0.##" (up to two).
        /// </summary>
        public static string Pattern(int minDecimals, int maxDecimals = -1)
        {
            if (maxDecimals < minDecimals)
            {
                maxDecimals = minDecimals;
            }

            return maxDecimals == 0
                ? "#,##0"
                : "#,##0." + new string('0', minDecimals) + new string('#', maxDecimals - minDecimals);
        }

        /// <summary>A number for display, grouped, with the PC's separators: <c>Number(1050.5m, 2)</c> = "1,050.50".</summary>
        public static string Number(decimal value, int minDecimals, int maxDecimals = -1) =>
            value.ToString(Pattern(minDecimals, maxDecimals), Display);

        /// <inheritdoc cref="Number(decimal, int, int)"/>
        public static string Number(double value, int minDecimals, int maxDecimals = -1) =>
            value.ToString(Pattern(minDecimals, maxDecimals), Display);

        /// <summary>A count for display ("2,190"); not for IDs.</summary>
        public static string Count(int value) => value.ToString(Pattern(0), Display);

        /// <summary>string.Format with <see cref="Display"/>; use grouped patterns (<see cref="Pattern"/>) for numbers.</summary>
        public static string Format(string format, params object[] args) => string.Format(Display, format, args);

        private static CultureInfo CreateWorking()
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            SetDates(culture);
            return CultureInfo.ReadOnly(culture);
        }

        internal static CultureInfo CreateDisplay(NumberFormatInfo pc)
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            SetDates(culture);
            var numbers = culture.NumberFormat;
            numbers.NumberDecimalSeparator = pc.NumberDecimalSeparator;
            numbers.NumberGroupSeparator = pc.NumberGroupSeparator;
            numbers.NumberGroupSizes = pc.NumberGroupSizes;
            numbers.PercentDecimalSeparator = pc.PercentDecimalSeparator;
            numbers.PercentGroupSeparator = pc.PercentGroupSeparator;

            // A PC set to use the same character for both would make "1.050" unreadable.
            if (numbers.NumberGroupSeparator == numbers.NumberDecimalSeparator)
            {
                numbers.NumberGroupSeparator = numbers.NumberDecimalSeparator == "," ? "." : ",";
            }

            return CultureInfo.ReadOnly(culture);
        }

        private static void SetDates(CultureInfo culture)
        {
            culture.DateTimeFormat.ShortDatePattern = DatePattern;
            culture.DateTimeFormat.LongDatePattern = DatePattern;
            culture.DateTimeFormat.DateSeparator = "/";
        }
    }
}
