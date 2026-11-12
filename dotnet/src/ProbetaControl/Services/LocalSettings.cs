using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProbetaControl.Services
{
    /// <summary>
    /// Small per-Windows-user conveniences (last user name), in %LOCALAPPDATA%\ProbetaControl\local.ini.
    /// Never holds anything important: a missing or broken file just means defaults.
    /// </summary>
    public static class LocalSettings
    {
        /// <summary>Replaceable so tests never touch the real file.</summary>
        internal static string FilePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProbetaControl", "local.ini");

        public static string LastUserName
        {
            get => Read().TryGetValue("LastUserName", out var value) ? value : null;
            set => Write("LastUserName", value);
        }

        /// <summary>"No volver a mostrar hoy" of the due-probetas warning: the day it was hidden.</summary>
        public static DateTime? DueWarningHiddenOn
        {
            get => Read().TryGetValue("DueWarningHiddenOn", out var value)
                   && DateTime.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                       System.Globalization.DateTimeStyles.None, out var day)
                ? day
                : (DateTime?)null;
            set => Write("DueWarningHiddenOn", value?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        }

        private static Dictionary<string, string> Read()
        {
            try
            {
                return File.Exists(FilePath)
                    ? File.ReadAllLines(FilePath)
                        .Select(line => line.Split(new[] { '=' }, 2))
                        .Where(parts => parts.Length == 2)
                        .GroupBy(parts => parts[0].Trim())
                        .ToDictionary(g => g.Key, g => g.Last()[1].Trim())
                    : new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static void Write(string key, string value)
        {
            try
            {
                var values = Read();
                values[key] = value ?? string.Empty;
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, values.Select(pair => pair.Key + "=" + pair.Value));
            }
            catch
            {
                // Convenience only.
            }
        }
    }

    /// <summary>Stored English values shown in Spanish.</summary>
    public static class DisplayText
    {
        public static string Role(string role)
        {
            switch (role)
            {
                case Models.UserRoles.Admin:
                    return "Administrador";
                case Models.UserRoles.Operator:
                    return "Operador";
                default:
                    return role;
            }
        }
    }
}
