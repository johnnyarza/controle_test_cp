using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ProbetaControl.Services
{
    /// <summary>The built-in help page (Help/ayuda.html), carried inside the exe.</summary>
    public static class HelpContent
    {
        public const string ResourceName = "Help.ayuda.html";

        private static string _html;

        /// <summary>The whole page (read once).</summary>
        public static string Load()
        {
            if (_html == null)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                    ?? throw new FileNotFoundException("Help page missing from the exe", ResourceName))
                using (var reader = new StreamReader(stream))
                {
                    _html = reader.ReadToEnd();
                }
            }

            return _html;
        }

        /// <summary>Every id in the page: the topics F1 can open.</summary>
        public static IReadOnlyCollection<string> TopicIds() =>
            new HashSet<string>(Regex.Matches(Load(), "\\sid=\"([^\"]+)\"").Cast<Match>().Select(m => m.Groups[1].Value));
    }
}
