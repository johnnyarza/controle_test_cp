using System;
using System.IO;
using MySqlConnector;

namespace ProbetaControl.Services
{
    /// <summary>
    /// "ProbetaControl.exe --selftest": proves the libraries packed inside the exe load and work
    /// (BCrypt, MySqlConnector, Dapper, DPAPI) without touching the database. Writes the result to
    /// the log and returns 0 when everything works. Used after building and by support.
    /// </summary>
    public static class SelfTest
    {
        public const string Switch = "--selftest";

        public static int Run()
        {
            try
            {
                var hash = BCrypt.Net.BCrypt.HashPassword("selftest", 4);
                if (!BCrypt.Net.BCrypt.Verify("selftest", hash))
                {
                    throw new InvalidOperationException("BCrypt verify failed");
                }

                using (var connection = new MySqlConnection("Server=localhost;Database=x"))
                {
                    GC.KeepAlive(connection);
                }

                GC.KeepAlive(typeof(Dapper.SqlMapper));

                var config = new ConfigStore(Path.Combine(Path.GetTempPath(), "ProbetaControl.selftest.config"));
                config.Save(new ConnectionSettings { Host = "h", UserName = "u", Password = "p" });
                var roundTrip = config.Load();
                File.Delete(config.Path);
                if (roundTrip.Password != "p")
                {
                    throw new InvalidOperationException("DPAPI round trip failed");
                }

                // Reports: MigraDoc lays out a page, GDI+ draws it, PDFsharp writes it.
                var report = new MigraDoc.DocumentObjectModel.Document();
                report.AddSection().AddParagraph("Probeta Control — prueba ñ ³ ±");
                var renderer = new Reports.ReportRenderer(report);
                using (renderer.RenderPage(1, 36))
                {
                }

                var pdfPath = Path.Combine(Path.GetTempPath(), "ProbetaControl.selftest.pdf");
                renderer.SavePdf(pdfPath);
                var pdfOk = new FileInfo(pdfPath).Length > 0;
                File.Delete(pdfPath);
                if (!pdfOk)
                {
                    throw new InvalidOperationException("PDF output failed");
                }

                var scripts = Data.Schema.SchemaScript.LoadEmbedded().Count;
                AppLog.Info($"Selftest OK: BCrypt, MySqlConnector, Dapper, DPAPI, PDFsharp/MigraDoc, {scripts} schema scripts");
                return 0;
            }
            catch (Exception ex)
            {
                AppLog.Error("Selftest FAILED", ex);
                return 1;
            }
        }
    }
}
