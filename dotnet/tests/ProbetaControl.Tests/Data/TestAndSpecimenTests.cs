using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;

namespace ProbetaControl.Tests.Data
{
    /// <summary>Compression tests and specimens: queries, filters and the legacy editing rules.</summary>
    public class TestAndSpecimenTests : ServiceFixture
    {
        public TestAndSpecimenTests(TestServer server)
            : base(server)
        {
        }

        [Test]
        public async Task Test_loads_with_client_provider_and_design_names()
        {
            var test = await NewTestAsync();
            var loaded = await Services.Tests.GetAsync(test.Id);

            Assert.That(loaded.ClientName, Does.StartWith("Cliente"));
            Assert.That(loaded.ConcreteProviderName, Is.EqualTo(loaded.ClientName));
            Assert.That(loaded.ConcreteDesignDescription, Does.StartWith("H-21"));
            Assert.That(loaded.TestDate, Is.EqualTo(Today.AddDays(-30)));
        }

        [Test]
        public async Task Specimens_load_with_calculated_values()
        {
            var test = await NewTestAsync();
            await Services.Specimens.SaveAsync(NewSpecimen(test.Id, Today.AddDays(-28), 28, load: 40), testUnlocked: true);

            var specimen = (await Services.Specimens.ListByTestAsync(test.Id)).Single();
            Assert.That(specimen.AgeDays, Is.EqualTo(28));
            Assert.That(specimen.DensityKgPerM3, Is.EqualTo(2357.8510).Within(0.0001));
            Assert.That(specimen.StrengthKgPerCm2, Is.EqualTo(226.3537).Within(0.0001));
            Assert.That(specimen.IsLocked, Is.True, "saving locks the measurements (legacy rule)");
        }

        [Test]
        public async Task Zero_rupture_load_is_stored_as_not_tested()
        {
            var test = await NewTestAsync();
            var specimen = NewSpecimen(test.Id, Today, 7, load: 0);
            await Services.Specimens.SaveAsync(specimen, testUnlocked: true);

            var stored = (await Services.Specimens.ListByTestAsync(test.Id)).Single();
            Assert.That(stored.RuptureLoadTon, Is.Null);
            Assert.That(stored.IsTested, Is.False);
        }

        [Test]
        public async Task Filter_by_id_ranges_and_mold_dates()
        {
            var test = await NewTestAsync();
            var ids = new int[6];
            for (var i = 0; i < 6; i++)
            {
                var s = NewSpecimen(test.Id, Today.AddDays(-i), 28);
                await Services.Specimens.SaveAsync(s, testUnlocked: true);
                ids[i] = s.Id;
            }

            IdRangeParser.TryParse($"{ids[0]}-{ids[1]}, {ids[4]}", out var ranges);
            var byId = await Services.Specimens.ListByTestAsync(test.Id, new SpecimenFilter { IdRanges = ranges });
            Assert.That(byId.Select(s => s.Id), Is.EqualTo(new[] { ids[0], ids[1], ids[4] }));

            var byDate = await Services.Specimens.ListByTestAsync(test.Id, new SpecimenFilter
            {
                MoldDateFrom = Today.AddDays(-3),
                MoldDateTo = Today.AddDays(-1),
            });
            Assert.That(byDate.Select(s => s.Id), Is.EquivalentTo(new[] { ids[1], ids[2], ids[3] }));

            var both = await Services.Specimens.ListByTestAsync(test.Id, new SpecimenFilter
            {
                IdRanges = ranges,
                MoldDateFrom = Today.AddDays(-3),
            });
            Assert.That(both.Select(s => s.Id), Is.EqualTo(new[] { ids[0], ids[1] }));
        }

        [Test]
        public async Task Due_counts_and_warning_list()
        {
            var test = await NewTestAsync();
            var overdue = NewSpecimen(test.Id, Today.AddDays(-30), 28);
            var dueToday = NewSpecimen(test.Id, Today.AddDays(-7), 7);
            var dueTomorrow = NewSpecimen(test.Id, Today.AddDays(-6), 7);
            var later = NewSpecimen(test.Id, Today, 28);
            var tested = NewSpecimen(test.Id, Today.AddDays(-30), 28, load: 40);
            foreach (var s in new[] { overdue, dueToday, dueTomorrow, later, tested })
            {
                await Services.Specimens.SaveAsync(s, testUnlocked: true);
            }

            Assert.That(await Services.Specimens.CountDueAsync(test.Id), Is.EqualTo(2), "overdue + due today");

            var warnings = (await Services.Specimens.ListDueAsync()).Where(d => d.CompressionTestId == test.Id).ToList();
            Assert.That(warnings.Select(w => w.Id), Is.EqualTo(new[] { overdue.Id, dueToday.Id, dueTomorrow.Id }));
            Assert.That(warnings[0].ClientName, Does.StartWith("Cliente"));
            Assert.That(warnings[0].GetStatus(Today), Is.EqualTo(SpecimenStatus.Overdue));

            var listed = await Services.Tests.SearchAsync(new CompressionTestQuery { Text = test.Id.ToString() });
            var row = listed.Items.Single(i => i.Id == test.Id);
            Assert.That(row.SpecimenCount, Is.EqualTo(5));
            Assert.That(row.DueCount, Is.EqualTo(2));
        }

        [Test]
        public async Task Operator_on_a_locked_test_may_only_record_the_rupture_load()
        {
            var test = await NewTestAsync();
            var specimen = NewSpecimen(test.Id, Today.AddDays(-7), 7);
            await Services.Specimens.SaveAsync(specimen, testUnlocked: true);

            var edit = specimen.Clone();
            edit.DiameterCm = 15.2m;
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Specimens.SaveAsync(edit, testUnlocked: false));

            var record = (await Services.Specimens.ListByTestAsync(test.Id)).Single();
            record.RuptureLoadTon = 38.5m;
            await Services.Specimens.SaveAsync(record, testUnlocked: false);

            Assert.That((await Services.Specimens.ListByTestAsync(test.Id)).Single().RuptureLoadTon, Is.EqualTo(38.5m));

            var again = (await Services.Specimens.ListByTestAsync(test.Id)).Single();
            again.RuptureLoadTon = 39m;
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Specimens.SaveAsync(again, testUnlocked: false),
                "once tested, only an administrator can change it");
        }

        [Test]
        public async Task Locked_test_refuses_overdue_specimens_and_new_specimens()
        {
            var test = await NewTestAsync();
            var overdue = NewSpecimen(test.Id, Today.AddDays(-10), 7);
            await Services.Specimens.SaveAsync(overdue, testUnlocked: true);

            overdue.RuptureLoadTon = 40;
            var late = Assert.ThrowsAsync<AccessDeniedException>(() => Services.Specimens.SaveAsync(overdue, testUnlocked: false));
            Assert.That(late.Message, Does.Contain("retrasada"));

            Assert.ThrowsAsync<AccessDeniedException>(() =>
                Services.Specimens.SaveAsync(NewSpecimen(test.Id, Today, 7), testUnlocked: false));
        }

        [Test]
        public async Task Only_administrators_archive_and_restore_tests_and_specimens()
        {
            var test = await NewTestAsync();
            var specimen = NewSpecimen(test.Id, Today, 7);
            await Services.Specimens.SaveAsync(specimen, testUnlocked: true);

            // Even the operator who created the test in this session (test unlocked for them) may not archive.
            await SignInNewOperatorAsync();
            Session.UnlockTest(test.Id);

            var denied = Assert.ThrowsAsync<AccessDeniedException>(() => Services.Specimens.SetArchivedAsync(specimen, true));
            Assert.That(denied.Message, Is.EqualTo(AppSession.ArchiveAdminOnly));
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Tests.SetArchivedAsync(test, true));
            Assert.That((await Services.Tests.GetAsync(test.Id)).IsArchived, Is.False, "nothing was archived");

            SignInAsAdmin();
            await Services.Specimens.SetArchivedAsync(specimen, true);
            await Services.Tests.SetArchivedAsync(test, true);

            await SignInNewOperatorAsync();
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Specimens.SetArchivedAsync(specimen, false), "restoring too");
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Tests.SetArchivedAsync(test, false), "restoring too");
        }

        [Test]
        public async Task Archived_specimens_leave_the_list_but_stay_in_the_database()
        {
            var test = await NewTestAsync();
            var specimen = NewSpecimen(test.Id, Today, 7);
            await Services.Specimens.SaveAsync(specimen, testUnlocked: true);
            await Services.Specimens.SetArchivedAsync(specimen, true);

            Assert.That(await Services.Specimens.ListByTestAsync(test.Id), Is.Empty);
            var all = await Services.Specimens.ListByTestAsync(test.Id, new SpecimenFilter { IncludeArchived = true });
            Assert.That(all.Single().GetStatus(Today), Is.EqualTo(SpecimenStatus.Archived));
        }

        [Test]
        public void Test_header_needs_client_provider_design_project_and_address()
        {
            var ex = Assert.ThrowsAsync<ValidationException>(() =>
                Services.Tests.SaveAsync(new CompressionTest { TestDate = Today }, testUnlocked: true));
            Assert.That(ex.Errors.Keys, Is.EquivalentTo(new[] { "ClientId", "ConcreteProviderId", "ConcreteDesignId", "ProjectName", "Address" }));
        }

        [Test]
        public async Task Editing_the_header_of_a_locked_test_is_refused()
        {
            var test = await NewTestAsync();
            test.Address = "Otra dirección";
            Assert.ThrowsAsync<AccessDeniedException>(() => Services.Tests.SaveAsync(test, testUnlocked: false));
        }
    }
}
