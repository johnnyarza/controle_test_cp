using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels;
using ProbetaControl.ViewModels.CompressionTests;

namespace ProbetaControl.Tests.ViewModels
{
    /// <summary>The "Probetas para romper" warning shown after login.</summary>
    public class DueWarningTests : ServiceFixture
    {
        public DueWarningTests(TestServer server)
            : base(server)
        {
        }

        [SetUp]
        public void ForgetHiddenWarning() => LocalSettings.DueWarningHiddenOn = null;

        private ShellViewModel NewShell(ScriptedDialogs dialogs) => new ShellViewModel(Services, dialogs, logout: () => { });

        /// <summary>A test with one overdue, two due today, one due tomorrow, one later and one broken probeta.</summary>
        private async Task<(CompressionTest Test, Specimen[] Due)> NewTestWithDueSpecimensAsync()
        {
            var test = await NewTestAsync();
            var overdue = NewSpecimen(test.Id, Today.AddDays(-30), 28);
            var today1 = NewSpecimen(test.Id, Today.AddDays(-7), 7);
            var today2 = NewSpecimen(test.Id, Today.AddDays(-7), 7);
            var tomorrow = NewSpecimen(test.Id, Today.AddDays(-13), 14);
            var later = NewSpecimen(test.Id, Today, 28);
            var broken = NewSpecimen(test.Id, Today.AddDays(-7), 7, load: 40);
            foreach (var s in new[] { overdue, today1, today2, tomorrow, later, broken })
            {
                await Services.Specimens.SaveAsync(s, testUnlocked: true);
            }

            return (test, new[] { overdue, today1, today2, tomorrow });
        }

        [Test]
        public async Task Warning_groups_the_due_probetas_by_ensayo_with_counts()
        {
            var (test, due) = await NewTestWithDueSpecimensAsync();
            var dialogs = new ScriptedDialogs();
            DueSpecimensViewModel shown = null;
            dialogs.Answer(dialog =>
            {
                shown = (DueSpecimensViewModel)dialog;
                return false;
            });

            await NewShell(dialogs).ShowDueWarningAsync();

            var group = shown.Groups.Single(g => g.TestId == test.Id);
            Assert.That(group.Heading, Is.EqualTo($"Ensayo Nº {test.Id} · Edificio Central"));
            Assert.That(group.Details, Does.StartWith("Cliente").And.Contain(" · H-21"));
            Assert.That(group.Rows.Select(r => r.Id), Is.EqualTo(due.Select(s => s.Id)), "most urgent first, later and broken ones left out");
            Assert.That(group.Rows.Select(r => r.StatusText), Is.EqualTo(new[] { "Retrasada", "Hoy", "Hoy", "Mañana" }));

            // Counts cover every ensayo in the database (other tests of this fixture may add some).
            Assert.That(shown.Total, Is.EqualTo(shown.Groups.Sum(g => g.Rows.Count)));
            Assert.That(shown.OverdueCount + shown.TodayCount + shown.TomorrowCount, Is.EqualTo(shown.Total));
            Assert.That(shown.Heading, Is.EqualTo($"Hay {shown.Total} probetas para romper"));
            Assert.That(shown.OpenTestId, Is.Null);
        }

        [Test]
        public async Task Abrir_ensayo_opens_its_detail_page_over_the_list()
        {
            var (test, _) = await NewTestWithDueSpecimensAsync();
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                ((DueSpecimensViewModel)dialog).Groups.Single(g => g.TestId == test.Id).OpenCommand.Execute(null);
                return true;
            });

            var shell = NewShell(dialogs);
            await shell.ShowDueWarningAsync();

            Assert.That(shell.CurrentPage, Is.InstanceOf<TestDetailPageViewModel>());
            Assert.That(((TestDetailPageViewModel)shell.CurrentPage).TestId, Is.EqualTo(test.Id));
        }

        [Test]
        public async Task Hidden_today_stays_hidden_until_tomorrow()
        {
            await NewTestWithDueSpecimensAsync();
            var dialogs = new ScriptedDialogs();
            dialogs.Answer(dialog =>
            {
                ((DueSpecimensViewModel)dialog).HideToday = true;
                return false;
            });

            await NewShell(dialogs).ShowDueWarningAsync();
            Assert.That(LocalSettings.DueWarningHiddenOn, Is.EqualTo(Today));

            // ScriptedDialogs fails on any dialog it was not told to expect.
            await NewShell(dialogs).ShowDueWarningAsync();
            Assert.That(dialogs.Shown, Has.Count.EqualTo(1));

            Clock.Today = Today.AddDays(1);
            dialogs.Answer(_ => false);
            await NewShell(dialogs).ShowDueWarningAsync();
            Assert.That(dialogs.Shown, Has.Count.EqualTo(2), "shown again the next day");
        }

        [Test]
        public async Task Nothing_due_shows_nothing()
        {
            await NewTestWithDueSpecimensAsync();
            Clock.Today = new DateTime(2000, 1, 1); // before every probeta's rupture date
            var dialogs = new ScriptedDialogs();
            await NewShell(dialogs).ShowDueWarningAsync();
            Assert.That(dialogs.Shown, Is.Empty);
        }
    }
}
