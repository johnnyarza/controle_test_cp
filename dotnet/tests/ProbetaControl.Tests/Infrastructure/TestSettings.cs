using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ProbetaControl.Tests.Infrastructure
{
    /// <summary>
    /// Database test settings from dotnet/tests/test.env (see test.env.example).
    /// Environment variables with the same names win over the file.
    /// </summary>
    public sealed class TestSettings
    {
        private static readonly Lazy<TestSettings> Instance = new Lazy<TestSettings>(Load);

        private TestSettings(IReadOnlyDictionary<string, string> values, string sourceFile)
        {
            SourceFile = sourceFile;
            AdminUser = Get(values, "TEST_ADMIN_USER");
            AdminPassword = Get(values, "TEST_ADMIN_PASSWORD");
            AppUser = Get(values, "TEST_APP_USER");
            AppPassword = Get(values, "TEST_APP_PASSWORD");
            Servers = (Get(values, "TEST_SERVERS") ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Select(TestServer.Parse)
                .ToList();
        }

        public static TestSettings Current => Instance.Value;

        public string SourceFile { get; }

        public string AdminUser { get; }

        public string AdminPassword { get; }

        public string AppUser { get; }

        public string AppPassword { get; }

        public IReadOnlyList<TestServer> Servers { get; }

        public bool IsConfigured =>
            Servers.Count > 0 && !string.IsNullOrEmpty(AdminUser) && !string.IsNullOrEmpty(AppUser);

        private static string Get(IReadOnlyDictionary<string, string> values, string key)
        {
            var fromEnvironment = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(fromEnvironment))
            {
                return fromEnvironment;
            }

            return values.TryGetValue(key, out var value) ? value : null;
        }

        private static TestSettings Load()
        {
            var file = FindEnvFile();
            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            if (file != null)
            {
                foreach (var rawLine in File.ReadAllLines(file))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var equals = line.IndexOf('=');
                    if (equals > 0)
                    {
                        values[line.Substring(0, equals).Trim()] = line.Substring(equals + 1).Trim();
                    }
                }
            }

            return new TestSettings(values, file);
        }

        /// <summary>Walks up from the test output folder to find tests/test.env.</summary>
        private static string FindEnvFile()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "test.env");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
