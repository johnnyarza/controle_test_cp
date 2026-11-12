using System.Globalization;

namespace ProbetaControl.Services
{
    /// <summary>
    /// Numbers typed in forms. Both "12,5" and "12.5" are accepted (office PCs and habits differ);
    /// thousands separators are not, so "1.234,5" is rejected instead of being misread.
    /// </summary>
    public static class NumberInput
    {
        public const string InvalidMessage = "Número no válido.";

        /// <summary>Empty text gives null and succeeds; text that is not a number fails.</summary>
        public static bool TryParse(string text, out decimal? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            var normalized = text.Trim().Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }

        /// <summary>Parses <paramref name="text"/>, adding an error for <paramref name="field"/> when it is not a number.</summary>
        public static decimal? Parse(string field, string text, ValidationErrors errors, bool required = false)
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

        /// <summary>For editing: no trailing zeros ("350", "12.5", "1.225").</summary>
        public static string Format(decimal? value) =>
            value?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
