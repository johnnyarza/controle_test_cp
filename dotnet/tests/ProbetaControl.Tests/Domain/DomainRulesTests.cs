using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProbetaControl.Data;
using ProbetaControl.Models;
using ProbetaControl.Services;

namespace ProbetaControl.Tests.Domain
{
    /// <summary>Business rules ported from the Java app. No database needed.</summary>
    [TestFixture]
    public class SpecimenMathTests
    {
        private static readonly DateTime Today = new DateTime(2026, 3, 10);

        [TestCase(15, 30, 12.5, 2357.8510)]
        [TestCase(10, 20, 3.6, 2291.8312)]
        public void Density_matches_legacy_formula(decimal diameter, decimal height, decimal weight, double expected)
        {
            Assert.That(SpecimenMath.DensityKgPerM3(diameter, height, weight), Is.EqualTo(expected).Within(0.0001));
        }

        [TestCase(15, 40, 226.3537)]
        [TestCase(10, 18.5, 235.5493)]
        public void Strength_matches_legacy_formula(decimal diameter, decimal loadTon, double expected)
        {
            Assert.That(SpecimenMath.StrengthKgPerCm2(diameter, loadTon), Is.EqualTo(expected).Within(0.0001));
        }

        [Test]
        public void Density_and_strength_are_empty_when_they_cannot_be_calculated()
        {
            Assert.That(SpecimenMath.DensityKgPerM3(0, 30, 12.5m), Is.Null);
            Assert.That(SpecimenMath.DensityKgPerM3(15, 0, 12.5m), Is.Null);
            Assert.That(SpecimenMath.StrengthKgPerCm2(15, null), Is.Null, "not tested yet");
            Assert.That(SpecimenMath.StrengthKgPerCm2(15, 0), Is.Null, "legacy 0 = not tested");
            Assert.That(SpecimenMath.StrengthKgPerCm2(0, 40), Is.Null);
        }

        [Test]
        public void Age_is_whole_days_between_mold_and_rupture()
        {
            Assert.That(SpecimenMath.AgeDays(new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)), Is.EqualTo(28));
            Assert.That(SpecimenMath.AgeDays(new DateTime(2026, 2, 1, 23, 0, 0), new DateTime(2026, 2, 8, 1, 0, 0)), Is.EqualTo(7));
        }

        [TestCase(-1, null, false, SpecimenStatus.Overdue)]
        [TestCase(0, null, false, SpecimenStatus.DueSoon)]
        [TestCase(1, null, false, SpecimenStatus.DueSoon)]
        [TestCase(2, null, false, SpecimenStatus.Pending)]
        [TestCase(-5, 40.0, false, SpecimenStatus.Tested)]
        [TestCase(-5, null, true, SpecimenStatus.Archived)]
        public void Status_drives_row_colours(int ruptureInDays, double? load, bool archived, SpecimenStatus expected)
        {
            var specimen = new Specimen
            {
                RuptureDate = Today.AddDays(ruptureInDays),
                RuptureLoadTon = (decimal?)load,
                IsArchived = archived,
            };
            Assert.That(specimen.GetStatus(Today), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class SpecimenEditPolicyTests
    {
        private static readonly DateTime Today = new DateTime(2026, 3, 10);

        [Test]
        public void Unlocked_test_allows_everything()
        {
            var tested = new Specimen { RuptureDate = Today.AddDays(-3), RuptureLoadTon = 40, IsLocked = true };
            Assert.That(SpecimenEditPolicy.CanEdit(tested, testUnlocked: true, Today), Is.SameAs(EditDecision.Full));
        }

        [Test]
        public void Locked_test_refuses_a_specimen_already_tested()
        {
            var tested = new Specimen { RuptureDate = Today, RuptureLoadTon = 40, IsLocked = true };
            var decision = SpecimenEditPolicy.CanEdit(tested, false, Today);
            Assert.That(decision.IsAllowed, Is.False);
            Assert.That(decision.Reason, Does.Contain("rotura registrada"));
        }

        [Test]
        public void Locked_test_refuses_an_overdue_specimen()
        {
            var overdue = new Specimen { RuptureDate = Today.AddDays(-1), IsLocked = true };
            var decision = SpecimenEditPolicy.CanEdit(overdue, false, Today);
            Assert.That(decision.IsAllowed, Is.False);
            Assert.That(decision.Reason, Does.Contain("retrasada"));
        }

        [Test]
        public void Locked_test_allows_only_the_rupture_load_of_a_saved_specimen_due_today()
        {
            var due = new Specimen { RuptureDate = Today, IsLocked = true };
            var decision = SpecimenEditPolicy.CanEdit(due, false, Today);
            Assert.That(decision.IsAllowed, Is.True);
            Assert.That(decision.IsRuptureLoadOnly, Is.True);
        }

        [Test]
        public void Adding_or_archiving_needs_an_unlocked_test()
        {
            Assert.That(SpecimenEditPolicy.CanChangeStructure(true).IsAllowed, Is.True);
            Assert.That(SpecimenEditPolicy.CanChangeStructure(false).IsAllowed, Is.False);
        }
    }

    [TestFixture]
    public class CalculatorAndParserTests
    {
        [Test]
        public void Design_replacement_is_a_percentage_of_the_origin_quantity()
        {
            Assert.That(Calculators.ReplacementQuantity(350m, 20m), Is.EqualTo(70m));
        }

        [Test]
        public void Average_of_two_measurements()
        {
            Assert.That(Calculators.Average(15.1m, 14.9m), Is.EqualTo(15.0m));
        }

        [Test]
        public void Rupture_date_adds_days_to_mold_date()
        {
            Assert.That(Calculators.RuptureDate(new DateTime(2026, 1, 31), 28), Is.EqualTo(new DateTime(2026, 2, 28)));
        }

        [Test]
        public void Id_ranges_accept_numbers_ranges_and_spaces()
        {
            Assert.That(IdRangeParser.TryParse(" 1-5, 8 ,10 - 12,7-3", out var ranges), Is.True);
            Assert.That(ranges.Select(r => (r.From, r.To)),
                Is.EqualTo(new[] { (1, 5), (8, 8), (10, 12), (3, 7) }));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Empty_id_filter_means_no_filter(string text)
        {
            Assert.That(IdRangeParser.TryParse(text, out var ranges), Is.True);
            Assert.That(ranges, Is.Empty);
        }

        [TestCase("1-2-3")]
        [TestCase("abc")]
        [TestCase("5; DROP TABLE Specimens")]
        [TestCase("-4")]
        [TestCase("0")]
        public void Invalid_id_filters_are_rejected(string text)
        {
            Assert.That(IdRangeParser.TryParse(text, out _), Is.False);
        }

        [Test]
        public void Server_version_text_is_parsed()
        {
            Assert.That(StartupChecker.ParseServerVersion("8.0.20-log"), Is.EqualTo(new Version(8, 0, 20)));
            Assert.That(StartupChecker.ParseServerVersion("8.4.13"), Is.EqualTo(new Version(8, 4, 13)));
        }
    }

    [TestFixture]
    public class AuditJsonTests
    {
        [Test]
        public void Json_escapes_text_and_formats_values_invariantly()
        {
            var json = AuditJson.Object(new Dictionary<string, object>
            {
                ["Name"] = "Hormigón \"Sur\"\\\n",
                ["Fck"] = 210.5m,
                ["Date"] = new DateTime(2026, 3, 10),
                ["Locked"] = true,
                ["Empty"] = null,
            });

            Assert.That(json, Is.EqualTo(
                "{\"Name\":\"Hormigón \\\"Sur\\\"\\\\\\n\",\"Fck\":210.5,\"Date\":\"2026-03-10\",\"Locked\":true,\"Empty\":null}"));
        }

        [Test]
        public void Diff_keeps_only_changed_fields()
        {
            var before = new Dictionary<string, object> { ["Name"] = "A", ["Phone"] = null, ["Fck"] = 210.50m };
            var after = new Dictionary<string, object> { ["Name"] = "B", ["Phone"] = "", ["Fck"] = 210.5m };

            var diff = AuditWriter.Diff(before, after);

            Assert.That(diff.Keys, Is.EqualTo(new[] { "Name" }), "\"\" equals null and 210.50 equals 210.5");
        }
    }

    [TestFixture]
    public class ConfigStoreTests
    {
        private string _path;

        [SetUp]
        public void CreatePath() => _path = Path.Combine(Path.GetTempPath(), "probeta-config-" + Guid.NewGuid().ToString("N") + ".config");

        [TearDown]
        public void DeleteFile()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        [Test]
        public void Settings_round_trip_and_the_file_does_not_contain_the_password()
        {
            var store = new ConfigStore(_path);
            Assert.That(store.Exists, Is.False);

            store.Save(new ConnectionSettings { Host = "servidor", Port = 3308, Database = "probeta_control", UserName = "specimen_app", Password = "S3cr3t-ñ" });
            var loaded = store.Load();

            Assert.That(loaded.Host, Is.EqualTo("servidor"));
            Assert.That(loaded.Port, Is.EqualTo(3308u));
            Assert.That(loaded.UserName, Is.EqualTo("specimen_app"));
            Assert.That(loaded.Password, Is.EqualTo("S3cr3t-ñ"));

            var text = File.ReadAllText(_path);
            Assert.That(text, Does.Not.Contain("S3cr3t"));
            Assert.That(text, Does.Not.Contain("specimen_app"));
        }

        [Test]
        public void Saving_again_replaces_the_settings()
        {
            var store = new ConfigStore(_path);
            store.Save(new ConnectionSettings { Host = "uno", UserName = "u", Password = "p" });
            store.Save(new ConnectionSettings { Host = "dos", UserName = "u", Password = "p" });
            Assert.That(store.Load().Host, Is.EqualTo("dos"));
        }

        [Test]
        public void Damaged_file_gives_a_spanish_message()
        {
            File.WriteAllText(_path, "<ProbetaControl version=\"1\"><Protected>AAAA</Protected></ProbetaControl>");
            var ex = Assert.Throws<ConfigException>(() => new ConfigStore(_path).Load());
            Assert.That(ex.Message, Does.Contain("otro equipo"));
        }
    }
}
