using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using ProbetaControl.Services;

namespace ProbetaControl.LegacyImport
{
    /// <summary>
    /// One-off import of the legacy cp_db into an empty new database (phase 7).
    ///   LegacyImport --check   reads everything, writes and checks it, then rolls back; prints the report
    ///   LegacyImport           the real import
    /// Source: --source-host/--source-port/--source-db/--source-user (default localhost:3306, cp_db, root), read-only.
    /// Target: --target-host/--target-port/--target-db/--target-user (default localhost:3306, probeta_control,
    /// specimen_admin). Passwords are asked for (or taken from LEGACY_SOURCE_PASSWORD / LEGACY_TARGET_PASSWORD), never saved.
    /// --report file (default import-report.txt).
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                var options = Options.Parse(args);
                options.Source.Password = Environment.GetEnvironmentVariable("LEGACY_SOURCE_PASSWORD")
                    ?? AskPassword($"Contraseña de {options.Source.UserName} en {options.Source.Host} ({options.Source.Database}): ");
                options.Target.Password = Environment.GetEnvironmentVariable("LEGACY_TARGET_PASSWORD")
                    ?? AskPassword($"Contraseña de {options.Target.UserName} en {options.Target.Host} ({options.Target.Database}): ");
                var report = ImportRunner.RunAsync(options.Source, options.Target, options.Check, Console.WriteLine).GetAwaiter().GetResult();
                File.WriteAllText(options.ReportPath, report, Encoding.UTF8);
                Console.WriteLine();
                Console.WriteLine(report);
                Console.WriteLine("Informe guardado en " + Path.GetFullPath(options.ReportPath));
                return 0;
            }
            catch (UsageException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 2;
            }
            catch (ImportException ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                return 1;
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine("ERROR de MySQL: " + ex.Message);
                return 1;
            }
        }

        private static string AskPassword(string prompt)
        {
            Console.Write(prompt);
            var text = new StringBuilder();
            for (var key = Console.ReadKey(true); key.Key != ConsoleKey.Enter; key = Console.ReadKey(true))
            {
                if (key.Key == ConsoleKey.Backspace && text.Length > 0)
                {
                    text.Length--;
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    text.Append(key.KeyChar);
                }
            }

            Console.WriteLine();
            return text.ToString();
        }
    }

    /// <summary>Read → plan → write (or check) → report text. Used by Program and the tests.</summary>
    public static class ImportRunner
    {
        public static async Task<string> RunAsync(ConnectionSettings source, ConnectionSettings target, bool check, Action<string> progress)
        {
            progress($"Leyendo {source.Database} en {source.Host}:{source.Port} (solo lectura)…");
            var data = await LegacyReader.ReadAsync(source.BuildConnectionString());
            var plan = ImportPlanner.Plan(data);
            progress(check
                ? $"Comprobando la importación en {target.Database} (no se guarda nada)…"
                : $"Importando en {target.Database}…");
            await ImportWriter.WriteAsync(target.BuildConnectionString(), plan, commit: !check);

            var text = new StringBuilder();
            text.AppendLine(check ? "COMPROBACIÓN (no se guardó nada)" : "IMPORTACIÓN REALIZADA");
            text.AppendLine(string.Format(CultureInfo.InvariantCulture, "Fecha: {0:dd/MM/yyyy HH:mm}", DateTime.Now));
            text.AppendLine($"Origen: {source.Database} en {source.Host}:{source.Port}   Destino: {target.Database} en {target.Host}:{target.Port}");
            text.AppendLine();
            text.Append(plan.Report.ToText());
            text.AppendLine("Próximos números (después de todos los del sistema anterior, también los borrados):");
            foreach (var pair in plan.NextIds)
            {
                text.AppendLine($"  {pair.Key}: {pair.Value}");
            }

            return text.ToString();
        }
    }

    internal sealed class UsageException : Exception
    {
        public UsageException(string message)
            : base(message)
        {
        }
    }

    internal sealed class Options
    {
        public ConnectionSettings Source { get; } = new ConnectionSettings { Host = "localhost", Database = "cp_db", UserName = "root" };

        public ConnectionSettings Target { get; } = new ConnectionSettings { Host = "localhost", UserName = "specimen_admin" };

        public bool Check { get; private set; }

        public string ReportPath { get; private set; } = "import-report.txt";

        public static Options Parse(string[] args)
        {
            var options = new Options();
            for (var i = 0; i < args.Length; i++)
            {
                string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException("Falta el valor de " + args[i]);
                switch (args[i])
                {
                    case "--check": options.Check = true; break;
                    case "--report": options.ReportPath = Next(); break;
                    case "--source-host": options.Source.Host = Next(); break;
                    case "--source-port": options.Source.Port = uint.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--source-db": options.Source.Database = Next(); break;
                    case "--source-user": options.Source.UserName = Next(); break;
                    case "--target-host": options.Target.Host = Next(); break;
                    case "--target-port": options.Target.Port = uint.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--target-db": options.Target.Database = Next(); break;
                    case "--target-user": options.Target.UserName = Next(); break;
                    default:
                        throw new UsageException("Uso: LegacyImport [--check] [--report archivo] [--source-host h] [--source-port p] " +
                            "[--source-db cp_db] [--source-user root] [--target-host h] [--target-port p] [--target-db probeta_control] " +
                            "[--target-user specimen_admin]");
                }
            }

            return options;
        }
    }
}
