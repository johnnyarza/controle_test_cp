using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using ProbetaControl.Data.Schema;
using ProbetaControl.Models;
using ProbetaControl.Services;

namespace ProbetaControl.DemoSeed
{
    /// <summary>
    /// Builds probeta_demo: schema scripts, the demo users and realistic sample data with dates around today,
    /// then a program copy in dotnet/demo-app connected to it. Accounts and passwords: dotnet/database/demo/DEMO-CREDENTIALS.md.
    /// </summary>
    internal static class Program
    {
        public const string Database = "probeta_demo";
        public const string AppUser = "demo_app";
        public const string AppPassword = "Demo-App-2026";
        public const string AdminUser = "demo_admin";
        public const string AdminPassword = "Demo-Admin-2026";
        public const string AdminProgramPassword = "demo-admin-1";
        public const string OperatorPassword = "demo-oper-1";

        private static int Main(string[] args)
        {
            try
            {
                var options = Options.Parse(args);
                RunAsync(options).GetAwaiter().GetResult();
                return 0;
            }
            catch (UsageException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                if (ex is MySqlException mysql && (mysql.ErrorCode == MySqlErrorCode.AccessDenied || mysql.ErrorCode == MySqlErrorCode.DatabaseAccessDenied))
                {
                    Console.Error.WriteLine("Has create-demo.sql been run as root on this server? (dotnet/database/demo)");
                }

                return 1;
            }
        }

        private static async Task RunAsync(Options options)
        {
            Console.WriteLine($"Servidor {options.Host}:{options.Port}, base de datos {Database}");
            var admin = new ConnectionSettings { Host = options.Host, Port = options.Port, Database = Database, UserName = AdminUser, Password = AdminPassword };
            var app = admin.WithCredentials(AppUser, AppPassword);

            if (options.Reset)
            {
                await ExecuteAsync(admin, null, $"DROP DATABASE IF EXISTS `{Database}`; " +
                    $"CREATE DATABASE `{Database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;");
                Console.WriteLine("Base de datos vaciada (--reset).");
            }

            var applied = await new SchemaMigrator(admin.BuildConnectionString()).MigrateAsync();
            Console.WriteLine($"Esquema: {applied.Count} script(s) aplicados, versión {SchemaInfo.RequiredVersion}.");

            if (await ScalarAsync(admin, "SELECT COUNT(*) FROM Clients") > 0)
            {
                throw new UsageException($"{Database} ya tiene datos. Use --reset para empezar de nuevo (borra todo lo de la demo).");
            }

            var started = DateTime.Now;
            await new DemoData(app, DateTime.Today).FillAsync();
            Console.WriteLine($"Datos de ejemplo creados en {(DateTime.Now - started).TotalSeconds:0} s.");

            WriteAppFolder(options.AppFolder, app);
            Console.WriteLine();
            Console.WriteLine($"Listo. Abra {Path.Combine(options.AppFolder, "ProbetaControl.exe")}");
            Console.WriteLine($"Usuarios: admin / {AdminProgramPassword}  ·  msilva, lrojas / {OperatorPassword}");
        }

        /// <summary>A copy of the program next to its own (encrypted, per-PC) connection file.</summary>
        private static void WriteAppFolder(string folder, ConnectionSettings app)
        {
            Directory.CreateDirectory(folder);
            var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProbetaControl.exe");
            File.Copy(exe, Path.Combine(folder, "ProbetaControl.exe"), overwrite: true);
            new ConfigStore(Path.Combine(folder, ConfigStore.FileName)).Save(app);
        }

        private static async Task ExecuteAsync(ConnectionSettings settings, string database, string sql)
        {
            var builder = new MySqlConnectionStringBuilder(settings.BuildConnectionString()) { Database = database ?? string.Empty };
            using (var connection = new MySqlConnection(builder.ConnectionString))
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(sql, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private static async Task<long> ScalarAsync(ConnectionSettings settings, string sql)
        {
            using (var connection = new MySqlConnection(settings.BuildConnectionString()))
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(sql, connection))
                {
                    return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
                }
            }
        }
    }

    internal sealed class Options
    {
        public string Host { get; private set; } = "localhost";

        public uint Port { get; private set; } = 3306;

        public bool Reset { get; private set; }

        public string AppFolder { get; private set; }

        public static Options Parse(string[] args)
        {
            var root = FindDotnetFolder();
            var options = new Options { AppFolder = Path.Combine(root ?? Environment.CurrentDirectory, "demo-app") };
            options.ReadTestEnv(root);
            for (var i = 0; i < args.Length; i++)
            {
                string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException("Falta el valor de " + args[i]);
                switch (args[i])
                {
                    case "--reset":
                        options.Reset = true;
                        break;
                    case "--host":
                        options.Host = Next();
                        break;
                    case "--port":
                        options.Port = uint.Parse(Next(), CultureInfo.InvariantCulture);
                        break;
                    case "--app-folder":
                        options.AppFolder = Path.GetFullPath(Next());
                        break;
                    default:
                        throw new UsageException("Uso: DemoSeed [--reset] [--host h] [--port p] [--app-folder carpeta]");
                }
            }

            return options;
        }

        /// <summary>This PC's MySQL is the first TEST_SERVERS entry of tests/test.env (3306 on one PC, 3308 on the other).</summary>
        private void ReadTestEnv(string root)
        {
            var file = root == null ? null : Path.Combine(root, "tests", "test.env");
            if (file == null || !File.Exists(file))
            {
                return;
            }

            var line = File.ReadAllLines(file).FirstOrDefault(l => l.TrimStart().StartsWith("TEST_SERVERS=", StringComparison.Ordinal));
            var first = line?.Split(new[] { '=' }, 2)[1].Split(',')[0].Trim();
            var parts = first?.Split(':');
            if (parts?.Length == 2 && uint.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var port))
            {
                Host = parts[0];
                Port = port;
            }
        }

        /// <summary>The repository's dotnet folder (holds ProbetaControl.sln), searched upwards from the tool.</summary>
        private static string FindDotnetFolder()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProbetaControl.sln")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName;
        }
    }

    internal sealed class UsageException : Exception
    {
        public UsageException(string message)
            : base(message)
        {
        }
    }
}
