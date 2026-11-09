using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProbetaControl.Data.Schema;

namespace ProbetaControl.Tests.Schema
{
    /// <summary>Rules every numbered script must follow. No database needed.</summary>
    [TestFixture]
    public class SchemaScriptTests
    {
        private static readonly SchemaScript[] Scripts = SchemaScript.LoadEmbedded().ToArray();

        [Test]
        public void Scripts_are_embedded_and_numbered_consecutively_from_1()
        {
            Assert.That(Scripts, Is.Not.Empty);
            Assert.That(Scripts.Select(s => s.Version), Is.EqualTo(Enumerable.Range(1, Scripts.Length)));
        }

        [Test]
        public void Required_version_is_the_last_script()
        {
            Assert.That(SchemaInfo.RequiredVersion, Is.EqualTo(Scripts.Last().Version));
        }

        [TestCaseSource(nameof(Scripts))]
        public void Script_ends_by_recording_its_own_version(SchemaScript script)
        {
            var lastStatement = script.Sql.TrimEnd().TrimEnd(';').Split(';').Last().Trim();
            var expected = $"INSERT INTO SchemaVersion (Version) VALUES ({script.Version})";
            Assert.That(lastStatement, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(Scripts))]
        public void Every_table_uses_utf8mb4_0900_ai_ci(SchemaScript script)
        {
            var creates = Regex.Matches(script.Sql, @"CREATE\s+TABLE\b.*?\)\s*ENGINE[^;]*;", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var tableCount = Regex.Matches(script.Sql, @"CREATE\s+TABLE\b", RegexOptions.IgnoreCase).Count;

            Assert.That(creates.Count, Is.EqualTo(tableCount), "every CREATE TABLE must end with an ENGINE/charset clause");
            foreach (Match create in creates)
            {
                Assert.That(create.Value, Does.Contain("DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci"));
            }
        }

        [TestCaseSource(nameof(Scripts))]
        public void Script_avoids_constructs_the_migrator_cannot_run(SchemaScript script)
        {
            Assert.That(script.Sql, Does.Not.Contain("DELIMITER").IgnoreCase, "the migrator sends the script as one batch");
            Assert.That(Regex.IsMatch(script.Sql, @"^\s*(USE|CREATE\s+DATABASE)\b", RegexOptions.Multiline | RegexOptions.IgnoreCase),
                Is.False, "scripts run inside the database chosen by the connection");
        }
    }
}
