using System.Collections.Generic;
using System.Globalization;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    /// <summary>Parses the specimen ID filter, e.g. "1-5, 8, 10-12" (single numbers allowed).</summary>
    public static class IdRangeParser
    {
        public const string InvalidMessage = "Use números y rangos separados por comas, por ejemplo: 1-5, 8, 10-12";

        public static bool TryParse(string text, out IReadOnlyList<IdRange> ranges)
        {
            var result = new List<IdRange>();
            ranges = result;

            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            foreach (var rawSegment in text.Split(','))
            {
                var segment = rawSegment.Replace(" ", string.Empty).Replace("\t", string.Empty);
                if (segment.Length == 0)
                {
                    continue;
                }

                var parts = segment.Split('-');
                if (parts.Length > 2 || !TryParseId(parts[0], out var from))
                {
                    return false;
                }

                var to = from;
                if (parts.Length == 2 && !TryParseId(parts[1], out to))
                {
                    return false;
                }

                result.Add(new IdRange(from, to));
            }

            return true;
        }

        private static bool TryParseId(string text, out int value) =>
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0;
    }
}
