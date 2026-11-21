using System;
using System.Linq;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.ViewModels.CompressionTests;

namespace ProbetaControl.Tests.Domain
{
    /// <summary>The probetas filter's one rule (page, counts and printouts), without a database.</summary>
    public class SpecimenFilterTests
    {
        private static readonly DateTime Today = new DateTime(2026, 3, 10);

        private static Specimen S(int id, DateTime mold, int age, decimal? load = null, string code = "P", bool archived = false) => new Specimen
        {
            Id = id, Code = code, MoldDate = mold, RuptureDate = mold.AddDays(age), DiameterCm = 15, HeightCm = 30,
            RuptureLoadTon = load, IsArchived = archived,
        };

        private static int[] Ids(SpecimenFilter filter, params Specimen[] specimens) =>
            specimens.Where(s => filter.Matches(s, Today)).Select(s => s.Id).ToArray();

        [Test]
        public void Estado_untested_due_and_tested()
        {
            var overdue = S(1, Today.AddDays(-8), 7);
            var dueToday = S(2, Today.AddDays(-7), 7);
            var tomorrow = S(3, Today.AddDays(-6), 7);
            var broken = S(4, Today.AddDays(-30), 7, load: 40);
            var zeroLoad = S(5, Today.AddDays(-30), 7, load: 0);
            var all = new[] { overdue, dueToday, tomorrow, broken, zeroLoad };

            Assert.That(Ids(new SpecimenFilter(), all), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(Ids(new SpecimenFilter { State = SpecimenStateFilter.Untested }, all), Is.EqualTo(new[] { 1, 2, 3, 5 }),
                "a load of 0 (legacy) is not broken");
            Assert.That(Ids(new SpecimenFilter { State = SpecimenStateFilter.Due }, all), Is.EqualTo(new[] { 1, 2, 5 }),
                "today or earlier, like the page's tile");
            Assert.That(Ids(new SpecimenFilter { State = SpecimenStateFilter.Tested }, all), Is.EqualTo(new[] { 4 }));
        }

        [Test]
        public void Dates_days_ages_numbers_and_text_must_all_match()
        {
            var a = S(10, new DateTime(2026, 3, 1), 7, code: "Losa eje 1-2");
            var b = S(11, new DateTime(2026, 3, 1), 28, code: "Losa eje 1-2");
            var c = S(12, new DateTime(2026, 3, 5), 7, code: "Columna Ñandú C-3");
            var all = new[] { a, b, c };

            Assert.That(Ids(new SpecimenFilter { DateFrom = new DateTime(2026, 3, 2) }, all), Is.EqualTo(new[] { 12 }), "mold date by default");
            Assert.That(Ids(new SpecimenFilter { DateField = SpecimenDateField.Rupture, DateFrom = new DateTime(2026, 3, 8), DateTo = new DateTime(2026, 3, 12) }, all),
                Is.EqualTo(new[] { 10, 12 }), "both ends inclusive");
            Assert.That(Ids(new SpecimenFilter { MoldDays = new[] { new DateTime(2026, 3, 5, 14, 30, 0) } }, all), Is.EqualTo(new[] { 12 }), "whole days");
            Assert.That(Ids(new SpecimenFilter { Ages = new[] { 28 } }, all), Is.EqualTo(new[] { 11 }));
            Assert.That(Ids(new SpecimenFilter { IdRanges = new[] { new IdRange(11, 20) } }, all), Is.EqualTo(new[] { 11, 12 }));
            Assert.That(Ids(new SpecimenFilter { CodeText = " nandu " }, all), Is.EqualTo(new[] { 12 }), "case and accents ignored");
            Assert.That(Ids(new SpecimenFilter { CodeText = "losa", Ages = new[] { 7 } }, all), Is.EqualTo(new[] { 10 }));
        }

        [Test]
        public void Archived_only_when_asked_and_a_copy_keeps_the_rest()
        {
            var archived = S(20, Today, 7, archived: true);
            var filter = new SpecimenFilter { Ages = new[] { 7 } };
            Assert.That(filter.Matches(archived, Today), Is.False);
            Assert.That(filter.HasCriteria, Is.True);
            var with = filter.WithArchived(true);
            Assert.That(with.Matches(archived, Today), Is.True);
            Assert.That(filter.IncludeArchived, Is.False, "the original is unchanged");
            Assert.That(new SpecimenFilter { IncludeArchived = true }.HasCriteria, Is.False);
        }

        [TestCase("345", "345", null)]
        [TestCase(" 341-345, 350 ", "341-345, 350", null)]
        [TestCase("C-3", "", "C-3")]
        [TestCase("columna", "", "columna")]
        [TestCase("341-", "", "341-")]
        [TestCase("  ", "", null)]
        public void Numbers_or_text_box(string typed, string ranges, string text)
        {
            var (parsedRanges, parsedText) = SpecimenFilterPanelViewModel.ParseNumbersOrText(typed);
            Assert.That(ProbetaControl.Services.IdRangeParser.Format(parsedRanges), Is.EqualTo(ranges));
            Assert.That(parsedText, Is.EqualTo(text));
        }
    }
}
