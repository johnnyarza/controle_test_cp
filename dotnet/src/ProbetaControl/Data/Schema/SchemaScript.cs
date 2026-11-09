using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ProbetaControl.Data.Schema
{
    /// <summary>One numbered schema script (001_name.sql) embedded in the exe.</summary>
    public sealed class SchemaScript
    {
        private const string ResourcePrefix = "DbScripts.";

        private SchemaScript(int version, string fileName, string sql)
        {
            Version = version;
            FileName = fileName;
            Sql = sql;
        }

        public int Version { get; }

        public string FileName { get; }

        public string Sql { get; }

        /// <summary>All embedded scripts, ordered by version.</summary>
        public static IReadOnlyList<SchemaScript> LoadEmbedded()
        {
            var assembly = typeof(SchemaScript).Assembly;
            return assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                .Select(name => Load(assembly, name))
                .OrderBy(script => script.Version)
                .ToList();
        }

        private static SchemaScript Load(Assembly assembly, string resourceName)
        {
            var fileName = resourceName.Substring(ResourcePrefix.Length);
            var underscore = fileName.IndexOf('_');
            if (underscore <= 0 || !int.TryParse(fileName.Substring(0, underscore), NumberStyles.None, CultureInfo.InvariantCulture, out var version))
            {
                throw new InvalidOperationException($"Schema script '{fileName}' must be named NNN_description.sql.");
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return new SchemaScript(version, fileName, reader.ReadToEnd());
            }
        }

        public override string ToString() => FileName;
    }
}
