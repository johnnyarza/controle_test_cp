using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NUnit.Framework;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.ViewModels.Settings;
using ProbetaControl.ViewModels.Startup;

namespace ProbetaControl.Tests.Help
{
    /// <summary>The built-in help page and which topic F1 opens, without a database.</summary>
    public class HelpPageTests
    {
        private static readonly string Html = HelpContent.Load();

        [Test]
        public void Page_is_inside_the_exe_and_asks_the_web_control_for_its_newest_mode()
        {
            Assert.That(Html, Does.Contain("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">"),
                "without it the Windows web control behaves like Internet Explorer 7");
            Assert.That(Html, Does.Contain("<meta charset=\"utf-8\">"));
            Assert.That(Html, Does.Contain("Etiquetas QR"));
        }

        [Test]
        public void Script_is_plain_ES5_for_Internet_Explorer_11()
        {
            var script = Regex.Match(Html, "<script>(.*?)</script>", RegexOptions.Singleline).Groups[1].Value;
            Assert.That(script, Is.Not.Empty);
            Assert.That(script, Does.Not.Contain("=>"), "arrow functions");
            Assert.That(script, Does.Not.Contain("`"), "template strings");
            Assert.That(Regex.IsMatch(script, @"\b(let|const|class)\s"), Is.False, "let / const / class");
        }

        [Test]
        public void Every_topic_the_program_opens_exists_in_the_page()
        {
            var ids = HelpContent.TopicIds();
            Assert.That(HelpTopics.All.Concat(new[] { HelpTopics.Start, "conexion" }).Where(t => !ids.Contains(t)), Is.Empty);
        }

        [Test]
        public void Ids_are_unique_and_every_link_and_contents_entry_leads_somewhere()
        {
            var all = Regex.Matches(Html, "\\sid=\"([^\"]+)\"").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            Assert.That(all.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key), Is.Empty, "duplicate ids");

            var links = Regex.Matches(Html, "href=\"#([^\"]+)\"").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            Assert.That(links, Is.Not.Empty);
            Assert.That(links.Where(l => !all.Contains(l)), Is.Empty, "links to missing topics");

            var sections = Regex.Matches(Html, "<section id=\"([^\"]+)\"").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            var contents = Regex.Matches(Regex.Match(Html, "<nav id=\"toc\".*?</nav>", RegexOptions.Singleline).Value, "href=\"#([^\"]+)\"")
                .Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            Assert.That(contents, Is.EqualTo(sections), "the contents list names every topic, in page order");
        }

        [Test]
        public void Every_screen_has_a_topic()
        {
            var screens = typeof(HelpTopics).Assembly.GetTypes().Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && (
                typeof(PageViewModel).IsAssignableFrom(t) || typeof(DialogViewModel).IsAssignableFrom(t)
                || typeof(SettingsSection).IsAssignableFrom(t)
                || (t.Namespace == typeof(StartupViewModel).Namespace && t.Name.EndsWith("ViewModel", StringComparison.Ordinal) && t != typeof(StartupViewModel))));

            var missing = screens.Where(t => !HelpTopics.Has(t) && !HelpTopics.NoTopicOfTheirOwn.Contains(t)).Select(t => t.Name).ToList();
            Assert.That(missing, Is.Empty, "add them to HelpTopics (and a topic to ayuda.html if needed)");
            Assert.That(screens.Count(), Is.GreaterThan(30));
        }

        [Test]
        public void Message_boxes_have_no_topic_so_F1_shows_the_screen_behind_them()
        {
            Assert.That(HelpTopics.For(new ConfirmDialogViewModel("t", "m", "Sí", false)), Is.Null);
            Assert.That(HelpTopics.For(null), Is.Null);
            Assert.That(HelpTopics.For(new object()), Is.Null);
        }

        /// <summary>
        /// The page in the same Windows web control the help window uses (Internet Explorer 11): the script runs without
        /// errors, opens topics and searches. Runs on its own STA thread with a window off screen.
        /// </summary>
        [Test]
        public void Script_works_in_the_Windows_web_control()
        {
            var results = new Dictionary<string, object>();
            Exception failure = null;
            var thread = new Thread(() =>
            {
                var window = new Window { Width = 900, Height = 600, Left = -3000, Top = -3000, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
                var browser = new WebBrowser();
                window.Content = browser;
                var dispatcher = Dispatcher.CurrentDispatcher;
                var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
                timeout.Tick += (_, __) =>
                {
                    failure = failure ?? new TimeoutException("The page did not load");
                    dispatcher.InvokeShutdown();
                };
                browser.LoadCompleted += (_, __) =>
                {
                    try
                    {
                        string Eval(string js) => Convert.ToString(browser.InvokeScript("eval", js));
                        results["mode"] = Eval("document.documentMode");
                        results["layout"] = Eval("Math.round(document.getElementById('content').getBoundingClientRect().left) + '|' + " +
                            "Math.round(document.getElementById('toc').getBoundingClientRect().right) + '|' + " +
                            "Math.round(document.getElementById('search').getBoundingClientRect().right)");
                        results["opened"] = browser.InvokeScript("showTopic", "etiquetas");
                        results["active"] = Eval("document.querySelector('#toc a.active').getAttribute('href')");

                        Eval("var s = document.getElementById('search'); s.value = 'dosificacion aplicar'; " +
                             "var e = document.createEvent('Event'); e.initEvent('input', true, true); s.dispatchEvent(e);");
                        results["visible"] = Eval("(function () { var n = [], s = document.getElementsByTagName('section'); " +
                            "for (var i = 0; i < s.length; i++) { if (s[i].className !== 'hidden') { n.push(s[i].id); } } return n.join(','); })()");
                        results["marks"] = Eval("document.querySelectorAll('#content mark').length");
                        results["info"] = Eval("document.getElementById('search-info').innerText");

                        browser.InvokeScript("showTopic", "ensayo-filtro");
                        results["cleared"] = Eval("document.getElementById('search').value + '|' + document.querySelectorAll('#content mark').length + '|' + document.querySelectorAll('section.hidden').length");
                        results["afterFilter"] = Eval("document.querySelector('#toc a.active').getAttribute('href')");
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                    }

                    dispatcher.InvokeShutdown();
                };
                window.Show();
                browser.NavigateToString(Html);
                timeout.Start();
                Dispatcher.Run();
                window.Close();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.That(thread.Join(TimeSpan.FromSeconds(40)), Is.True, "the test thread finished");

            Assert.That(failure, Is.Null, failure?.ToString());
            Assert.That(results["mode"], Is.EqualTo("11"), "IE11 standards mode, not IE7");
            var layout = ((string)results["layout"]).Split('|').Select(int.Parse).ToArray();
            Assert.That(layout[0], Is.GreaterThanOrEqualTo(layout[1]), "the topics start right of the contents list, not under it");
            Assert.That(layout[2], Is.LessThan(layout[1]), "the search box fits in the contents list");
            Assert.That(results["opened"], Is.EqualTo(true));
            Assert.That(results["active"], Is.EqualTo("#etiquetas"));
            Assert.That(results["visible"], Is.EqualTo("dosificaciones"), "accents ignored: 'dosificacion' finds 'Dosificación'");
            Assert.That(int.Parse((string)results["marks"]), Is.GreaterThanOrEqualTo(2), "the words are marked");
            Assert.That(results["info"], Does.StartWith("1 tema"));
            Assert.That(results["cleared"], Is.EqualTo("|0|0"), "opening a topic clears the search");
            Assert.That(results["afterFilter"], Is.EqualTo("#ensayo"), "a sub-topic marks its topic in the contents");
        }
    }

    /// <summary>F1 / "?" in the main window, with a real database behind the pages.</summary>
    public class HelpCommandTests : ServiceFixture
    {
        public HelpCommandTests(TestServer server)
            : base(server)
        {
        }

        [Test]
        public async Task Help_opens_at_the_topic_of_the_page_on_screen()
        {
            var opened = new List<string>();
            var shell = new ShellViewModel(Services, new ScriptedDialogs(), logout: () => { }, showHelp: opened.Add);
            await Wait.UntilAsync(() => shell.CurrentPage != null);

            Assert.That(shell.HelpCommand.CanExecute(null), Is.True);
            shell.HelpCommand.Execute(null);
            Assert.That(opened.Last(), Is.EqualTo("ensayos"));

            shell.SelectedNavigationItem = shell.NavigationItems.Single(n => n.Title == "Dosificaciones");
            shell.HelpCommand.Execute(null);
            Assert.That(opened.Last(), Is.EqualTo("dosificaciones"));

            shell.SelectedNavigationItem = shell.NavigationItems.Single(n => n.Title == "Configuración");
            var settings = (SettingsPageViewModel)shell.CurrentPage;
            settings.SelectedSection = settings.Find<LabelsSectionViewModel>();
            shell.HelpCommand.Execute(null);
            Assert.That(opened.Last(), Is.EqualTo("config-etiquetas"), "Configuración opens its section's topic");

            var noHelp = new ShellViewModel(Services, new ScriptedDialogs(), logout: () => { });
            Assert.That(noHelp.HelpCommand.CanExecute(null), Is.False);
        }
    }
}
