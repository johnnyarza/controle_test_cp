using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ProbetaControl.Services
{
    /// <summary>
    /// Dates typed in forms, always day first: "05/11/2026", "5/11/26", "5-11-2026" and "5.11.2026" are all
    /// 5 November 2026. The PC's regional settings are never used, so a date is never read month first.
    /// </summary>
    public static class DateInput
    {
        public const string InvalidMessage = "Fecha no válida (dd/mm/aaaa).";

        private static readonly Regex Pattern = new Regex(@"^\s*(\d{1,2})\s*[/\-.]\s*(\d{1,2})\s*[/\-.]\s*(\d{2}|\d{4})\s*$");

        /// <summary>Empty text gives null and succeeds; text that is not a valid date fails.</summary>
        public static bool TryParse(string text, out DateTime? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            var match = Pattern.Match(text);
            if (!match.Success)
            {
                return false;
            }

            var day = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var year = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            if (match.Groups[3].Value.Length == 2)
            {
                year += 2000;
            }

            if (year < 1900 || year > 2999 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            {
                return false;
            }

            value = new DateTime(year, month, day);
            return true;
        }

        /// <summary>Parses <paramref name="text"/>, adding an error for <paramref name="field"/> when it is not a date.</summary>
        public static DateTime? Parse(string field, string text, ValidationErrors errors, bool required = false)
        {
            if (!TryParse(text, out var value))
            {
                errors.Add(field, InvalidMessage);
            }
            else if (required && value == null)
            {
                errors.Add(field, "Campo obligatorio.");
            }

            return value;
        }

        /// <summary>"05/11/2026"; empty for null.</summary>
        public static string Format(DateTime? value) => AppCulture.FormatDate(value);
    }
}
