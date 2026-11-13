using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.Tests.Infrastructure;
using ProbetaControl.ViewModels.CompressionTests;
using ProbetaControl.ViewModels.Infrastructure;
using ProbetaControl.ViewModels.Reports;
using ProbetaControl.ViewModels.Settings;

namespace ProbetaControl.Tests.Reports
{
    /// <summary>Lab details in the database, and "Imprimir" on the ensayo page.</summary>
    public class ReportDataTests : ServiceFixture
    {
        public ReportDataTests(TestServer server)
            : base(server)
        {
        }

        [Test]
        public async Task Lab_details_start_with_the_legacy_text_and_a4()
        {
            var settings = await Services.ReportSettings.GetAsync();

            Assert.That(settings.LabName, Is.EqualTo("Servicios de Ingeniería HJVA Ltda."));
            Assert.That(settings.FooterText, Does.StartWith("Pto. Quijarro, Av. Bolivia, 210"));
            Assert.That(settings.NoteText, Does.StartWith("NOTA:"));
            Assert.That(settings.PaperSize, Is.EqualTo(ReportPaperSize.A4));
        }

        [Test]
        public async Task Administrator_changes_lab_details_for_every_pc_and_operators_cannot()
        {
            var original = await Services.ReportSettings.GetAsync();
            try
            {
                await Services.ReportSettings.SaveAsync(new ReportSettings
                {
                    LabName = "  Laboratorio Prueba  ",
                    FooterText = "Calle 1",
                    NoteText = "   ",
                    PaperSize = ReportPaperSize.Letter,
                });

                var otherPc = new AppServices(Database.AppConnectionSettings, new AppSession("OTRO-PC"), Clock);
                var read = await otherPc.ReportSettings.GetAsync();
                Assert.That(read.LabName, Is.EqualTo("Laboratorio Prueba"), "trimmed");
                Assert.That(read.FooterText, Is.EqualTo("Calle 1"));
                Assert.That(read.NoteText, Is.Null, "blank = no note");
                Assert.That(read.PaperSize, Is.EqualTo(ReportPaperSize.Letter));

                using (var connection = await OpenAsAppAsync())
                {
                    var audited = await Dapper.SqlMapper.QueryAsync<string>(connection,
                        "SELECT a.Changes FROM AuditLog a JOIN AppSettings s ON s.Id = a.RecordId " +
                        "WHERE a.TableName = 'AppSettings' AND s.SettingKey = 'ReportPaperSize'");
                    Assert.That(audited.Last(), Does.Contain("Letter"));
                }

                var empty = Assert.ThrowsAsync<ValidationException>(() => Services.ReportSettings.SaveAsync(new ReportSettings { LabName = " " }));
                Assert.That(empty.Errors.Keys, Does.Contain("LabName"));
                var tooLong = Assert.ThrowsAsync<ValidationException>(() => Services.ReportSettings.SaveAsync(
                    new ReportSettings { LabName = "Lab", NoteText = new string('x', ReportSettings.MaxTextLength + 1) }));
                Assert.That(tooLong.Errors.Keys, Does.Contain("NoteText"));

                await SignInNewOperatorAsync();
                Assert.ThrowsAsync<AccessDeniedException>(() => Services.ReportSettings.SaveAsync(new ReportSettings { LabName = "Otro" }));
            }
            finally
            {
                SignInAsAdmin();
                await Services.ReportSettings.SaveAsync(original);
            }
        }

        [Test]
        public async Task Settings_section_loads_and_saves_the_lab_details()
        {
            var original = await Services.ReportSettings.GetAsync();
            try
            {
                var section = new ReportTextsSectionViewModel(Services.ReportSettings);
                await section.LoadAsync();
                Assert.That(section.LabName, Is.EqualTo(original.LabName));
                Assert.That(section.IsA4, Is.True);

                section.LabName = "";
                ((AsyncRelayCommand)section.SaveCommand).ExecuteAsync().GetAwaiter().GetResult();
                Assert.That(section.Errors["LabName"], Is.EqualTo("Campo obligatorio."));

                section.LabName = "Laboratorio Sección";
                section.IsLetter = true;
                ((AsyncRelayCommand)section.SaveCommand).ExecuteAsync().GetAwaiter().GetResult();
                Assert.That(section.Message, Does.StartWith("Datos guardados"));
                Assert.That((await Services.ReportSettings.GetAsync()).PaperSize, Is.EqualTo(ReportPaperSize.Letter));
            }
            finally
            {
                await Services.ReportSettings.SaveAsync(original);
            }
        }

        [Test]
        public async Task Imprimir_prints_the_filtered_probetas_without_archived_ones_and_saves_a_pdf()
        {
            var test = await NewTestAsync();
            var ids = new int[5];
            for (var i = 0; i < 5; i++)
            {
                var specimen = NewSpecimen(test.Id, Today.AddDays(-30), 28, load: 40);
                await Services.Specimens.SaveAsync(specimen, testUnlocked: true);
                ids[i] = specimen.Id;
            }

            await Services.Specimens.SetArchivedAsync(await Services.Specimens.GetAsync(ids[1]), true);

            // Whole ensayo: 4 active probetas.
            var whole = await Services.Reports.BuildTestReportAsync(test.Id, null);
            Assert.That(ReportRenderingTests.ResultsTable(whole).Rows.Count - 1, Is.EqualTo(4));

            // The page prints what its filter shows.
            var dialogs = new ScriptedDialogs();
            var page = new TestDetailPageViewModel(Services, dialogs, new RecordingNavigator(), test.Id);
            await page.OnNavigatedToAsync();
            dialogs.Answer(d =>
            {
                var filter = (SpecimenFilterViewModel)d;
                filter.IdRanges = $"{ids[0]}-{ids[2]}";
                filter.ApplyCommand.Execute(null);
                return true;
            });
            await ((AsyncRelayCommand)page.FilterCommand).ExecuteAsync();
            Assert.That(page.HasFilter, Is.True);
            Assert.That(page.PrintToolTip, Does.Contain("filtro"));

            var path = Path.Combine(Path.GetTempPath(), "ProbetaControl-test-" + Guid.NewGuid().ToString("N") + ".pdf");
            ReportPreviewViewModel preview = null;
            dialogs.AnswerFile(path);
            dialogs.AnswerPrinter(null);
            dialogs.Answer(d =>
            {
                preview = (ReportPreviewViewModel)d;
                ((AsyncRelayCommand)preview.SavePdfCommand).ExecuteAsync().GetAwaiter().GetResult();
                ((AsyncRelayCommand)preview.PrintCommand).ExecuteAsync().GetAwaiter().GetResult();
                return false;
            });
            try
            {
                page.PrintCommand.Execute(null);

                Assert.That(preview.Title, Is.EqualTo($"Informe del ensayo Nº {test.Id}"));
                Assert.That(preview.PageCount, Is.EqualTo(1));
                Assert.That(preview.Pages.Single().Width, Is.GreaterThan(preview.Pages.Single().Height), "landscape");
                Assert.That(dialogs.SuggestedFileNames.Single(), Does.StartWith("2026-02-08 Ensayo " + test.Id + " - "));
                Assert.That(File.Exists(path), Is.True, "Guardar PDF…");
                Assert.That(preview.Message, Does.StartWith("PDF guardado"), "printing was cancelled, so the message stays");
            }
            finally
            {
                File.Delete(path);
            }

            // ids[0]..ids[2] minus the archived ids[1].
            var filtered = await Services.Reports.BuildTestReportAsync(test.Id, page.Filter);
            Assert.That(ReportRenderingTests.ResultsTable(filtered).Rows.Count - 1, Is.EqualTo(2));
        }
    }
}
