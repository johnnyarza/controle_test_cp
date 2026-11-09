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
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProbetaControl", "local.ini");

        public static string LastUserName
        {
            get => Read().TryGetValue("LastUserName", out var value) ? value : null;
            set => Write("LastUserName", value);
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
