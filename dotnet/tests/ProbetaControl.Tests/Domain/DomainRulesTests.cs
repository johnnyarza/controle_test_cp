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

namespace ProbetaControl.Tests.Domain
{
    [TestFixture]
    public class NumberInputTests
    {
        [TestCase("12.5", 12.5)]
        [TestCase("12,5", 12.5)]
        [TestCase(" 350 ", 350)]
        [TestCase("-1", -1)]
        public void Accepts_point_or_comma_as_decimal_separator(string text, decimal expected)
        {
            Assert.That(NumberInput.TryParse(text, out var value), Is.True);
            Assert.That(value, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Empty_text_is_no_value(string text)
        {
            Assert.That(NumberInput.TryParse(text, out var value), Is.True);
            Assert.That(value, Is.Null);
        }

        [TestCase("1.234,5")]
        [TestCase("1,234.5")]
        [TestCase("25O")]
        [TestCase("12 kg")]
        public void Rejects_thousands_separators_and_letters(string text)
        {
            Assert.That(NumberInput.TryParse(text, out _), Is.False);
        }

        [Test]
        public void Formats_without_trailing_zeros()
        {
            Assert.That(NumberInput.Format(350.000m), Is.EqualTo("350"));
            Assert.That(NumberInput.Format(12.50m), Is.EqualTo("12.5"));
            Assert.That(NumberInput.Format(1.2254m), Is.EqualTo("1.225"));
            Assert.That(NumberInput.Format(null), Is.Empty);
        }

        [Test]
        public void Design_without_materials_is_rejected()
        {
            var errors = ConcreteDesignService.Validate(new ConcreteDesign { Description = "H-21", Fck = 210 });
            var ex = Assert.Throws<ValidationException>(errors.ThrowIfAny);
            Assert.That(ex.Errors.Keys, Is.EquivalentTo(new[] { "Materials" }));
        }
    }

    public class MaterialTypeAndUsageTests
    {
        [TestCase(190, 340, 0.56)]
        [TestCase(175, 350, 0.5)]
        [TestCase(200, 300, 0.67)]
        public void Water_cement_ratio_is_rounded_to_two_decimals(decimal water, decimal cement, decimal expected)
        {
            Assert.That(Calculators.WaterCementRatio(water, cement), Is.EqualTo(expected));
        }

        [TestCase(0, 340)]
        [TestCase(190, 0)]
        public void Water_cement_ratio_needs_both_materials(decimal water, decimal cement)
        {
            Assert.That(Calculators.WaterCementRatio(water, cement), Is.Null);
        }

        [Test]
        public void Unknown_material_types_show_as_otro()
        {
            Assert.That(MaterialTypes.Label(MaterialTypes.Admixture), Is.EqualTo("Aditivo"));
            Assert.That(MaterialTypes.Label("sand"), Is.EqualTo("Otro"));
            Assert.That(MaterialTypes.IsValid("sand"), Is.False);
            Assert.That(new Material().MaterialType, Is.EqualTo(MaterialTypes.Other));
        }

        [Test]
        public void In_use_message_lists_the_records_and_how_many_more()
        {
            var one = InUseException.BuildMessage("el material \"Cemento\"", new RecordUsage(1, new[] { "H-21" }),
                "dosificación activa", "dosificaciones activas", "Consejo.");
            Assert.That(one, Is.EqualTo("No se puede archivar el material \"Cemento\" porque se usa en 1 dosificación activa:\n• H-21\n\nConsejo."));

            var many = InUseException.BuildMessage("x", new RecordUsage(8, new[] { "a", "b", "c", "d", "e" }), "s", "p", "C.");
            Assert.That(many, Does.Contain("se usa en 8 p:").And.Contain("• e\n• … y 3 más\n\nC."));
        }
    }

    /// <summary>What the Dosificaciones list and its materials panel show.</summary>
    [TestFixture]
    public class DesignListValuesTests
    {
        [Test]
        public void Materials_panel_lists_used_slots_in_order_with_total_and_type()
        {
            var design = new ConcreteDesign
            {
                Materials =
                {
                    new ConcreteDesignMaterial { Position = 3, MaterialId = 7, MaterialName = "Grava", QuantityKgPerM3 = 1000, MaterialType = MaterialTypes.Aggregate },
                    new ConcreteDesignMaterial { Position = 1, MaterialId = 5, MaterialName = "Cemento", QuantityKgPerM3 = 350.5m, MaterialType = MaterialTypes.Cement },
                    new ConcreteDesignMaterial { Position = 2, MaterialId = null, QuantityKgPerM3 = 99 },
                },
            };

            Assert.That(design.UsedMaterials.Select(m => m.MaterialName), Is.EqualTo(new[] { "Cemento", "Grava" }));
            Assert.That(design.MaterialCount, Is.EqualTo(2));
            Assert.That(design.TotalKgPerM3, Is.EqualTo(1350.5m), "empty slots don't count");
            Assert.That(design.UsedMaterials[0].TypeLabel, Is.EqualTo("Cemento"));
            Assert.That(design.MaterialsSummary, Is.EqualTo("Cemento 350.5 · Grava 1000"));
        }

        [Test]
        public void NotEmpty_converter_can_be_inverted()
        {
            var converter = new ProbetaControl.Views.NotEmptyToVisibilityConverter();
            Assert.That(converter.Convert(null, null, null, null).ToString(), Is.EqualTo("Collapsed"));
            Assert.That(converter.Convert(new object(), null, null, null).ToString(), Is.EqualTo("Visible"));
            Assert.That(converter.Convert(null, null, "Invert", null).ToString(), Is.EqualTo("Visible"));
            Assert.That(converter.Convert("x", null, "Invert", null).ToString(), Is.EqualTo("Collapsed"));
        }
    }

    /// <summary>Typed dates are always day first, whatever the PC's regional settings.</summary>
    [TestFixture]
    public class DateInputTests
    {
        [TestCase("05/11/2026", 2026, 11, 5)]
        [TestCase("5/11/2026", 2026, 11, 5)]
        [TestCase(" 5-11-26 ", 2026, 11, 5)]
        [TestCase("5.11.2026", 2026, 11, 5)]
        [TestCase("29/02/2028", 2028, 2, 29)]
        public void Reads_day_month_year(string text, int year, int month, int day)
        {
            Assert.That(DateInput.TryParse(text, out var value), Is.True);
            Assert.That(value, Is.EqualTo(new DateTime(year, month, day)));
        }

        [TestCase("31/02/2026")]
        [TestCase("13/13/2026")]
        [TestCase("2026-11-05")]
        [TestCase("5/11")]
        [TestCase("hoy")]
        [TestCase("05/11/202")]
        public void Rejects_what_is_not_a_date(string text)
        {
            Assert.That(DateInput.TryParse(text, out _), Is.False);
            var errors = new ValidationErrors();
            DateInput.Parse("TestDate", text, errors);
            Assert.That(errors.HasErrors, Is.True);
        }

        [Test]
        public void Empty_is_null_unless_required_and_format_is_dd_mm_yyyy()
        {
            Assert.That(DateInput.TryParse("  ", out var value), Is.True);
            Assert.That(value, Is.Null);

            var errors = new ValidationErrors();
            DateInput.Parse("TestDate", "", errors, required: true);
            var ex = Assert.Throws<ValidationException>(() => errors.ThrowIfAny());
            Assert.That(ex.Errors["TestDate"], Is.EqualTo("Campo obligatorio."));

            Assert.That(DateInput.Format(new DateTime(2026, 3, 8)), Is.EqualTo("08/03/2026"));
            Assert.That(DateInput.Format(null), Is.Empty);
        }

        [Test]
        public void Validation_errors_can_move_to_the_form_field()
        {
            var errors = new ValidationErrors().Add("ClientId", "Seleccione el cliente.").Rename("ClientId", "ClientName");
            var ex = Assert.Throws<ValidationException>(() => errors.ThrowIfAny());
            Assert.That(ex.Errors.Keys, Is.EqualTo(new[] { "ClientName" }));
        }
    }
}
