using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using MySqlConnector;
using ProbetaControl.Data.Schema;

namespace ProbetaControl.Services
{
    public enum StartupStatus
    {
        Ready,
        NeedsConnectionSettings,
        CannotConnect,
        UnsupportedServer,
        DatabaseNeedsUpdate,
        ProgramNeedsUpdate,
    }

    public sealed class StartupResult
    {
        public StartupResult(StartupStatus status, string message = null, ConnectionSettings settings = null, int databaseVersion = 0)
        {
            Status = status;
            Message = message;
            Settings = settings;
            DatabaseVersion = databaseVersion;
        }

        public StartupStatus Status { get; }

        /// <summary>Spanish explanation for the user.</summary>
        public string Message { get; }

        /// <summary>The loaded settings (null when there are none or they can't be read).</summary>
        public ConnectionSettings Settings { get; }

        public int DatabaseVersion { get; }
    }

    /// <summary>
    /// Everything that must be true before the login window: .NET 4.8, a readable config file,
    /// a reachable MySQL 8.0.16+ server and a database at exactly the schema version of this build.
    /// </summary>
    public sealed class StartupChecker
    {
        /// <summary>Registry "Release" value of .NET Framework 4.8.</summary>
        public const int DotNet48Release = 528040;

        private static readonly Version MinimumServerVersion = new Version(8, 0, 16);

        private readonly ConfigStore _config;

        public StartupChecker(ConfigStore config)
        {
            _config = config;
        }

        /// <summary>Runs before anything else; the installer normally guarantees it.</summary>
        public static bool IsDotNet48Installed()
        {
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
            {
                return key?.GetValue("Release") is int release && release >= DotNet48Release;
            }
        }

        public async Task<StartupResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            if (!_config.Exists)
            {
                return new StartupResult(StartupStatus.NeedsConnectionSettings,
                    "Todavía no se configuró la conexión a la base de datos.");
            }

            ConnectionSettings settings;
            try
            {
                settings = _config.Load();
            }
            catch (ConfigException ex)
            {
                AppLog.Error("Config could not be read", ex);
                return new StartupResult(StartupStatus.NeedsConnectionSettings, ex.Message);
            }

            return await CheckDatabaseAsync(settings, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<StartupResult> CheckDatabaseAsync(ConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var connection = new MySqlConnection(settings.BuildConnectionString()))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    var serverVersion = ParseServerVersion(connection.ServerVersion);
                    if (serverVersion < MinimumServerVersion)
                    {
                        return new StartupResult(StartupStatus.UnsupportedServer,
                            $"El servidor MySQL es la versión {connection.ServerVersion}; se necesita 8.0.16 o posterior.", settings);
                    }
                }

                var version = await new SchemaMigrator(settings.BuildConnectionString())
                    .GetCurrentVersionAsync(cancellationToken).ConfigureAwait(false);

                if (version < SchemaInfo.RequiredVersion)
                {
                    return new StartupResult(StartupStatus.DatabaseNeedsUpdate,
                        $"La base de datos está en la versión {version} y este programa necesita la {SchemaInfo.RequiredVersion}. " +
                        "Un administrador debe actualizarla.", settings, version);
                }

                if (version > SchemaInfo.RequiredVersion)
                {
                    return new StartupResult(StartupStatus.ProgramNeedsUpdate,
                        $"La base de datos está en la versión {version}, más nueva que este programa ({SchemaInfo.RequiredVersion}). " +
                        "Instale la versión más reciente del programa.", settings, version);
                }

                return new StartupResult(StartupStatus.Ready, null, settings, version);
            }
            catch (MySqlException ex)
            {
                AppLog.Error("Database check failed", ex);
                return new StartupResult(StartupStatus.CannotConnect, DescribeConnectionError(ex), settings);
            }
        }

        internal static Version ParseServerVersion(string text)
        {
            var numeric = (text ?? string.Empty).Split('-')[0];
            return Version.TryParse(numeric, out var version) ? version : new Version(0, 0);
        }

        private static string DescribeConnectionError(MySqlException ex)
        {
            switch (ex.ErrorCode)
            {
                case MySqlErrorCode.UnableToConnectToHost:
                    return "No se pudo conectar al servidor de base de datos. Revise que el servidor esté encendido y la red funcione.";
                case MySqlErrorCode.AccessDenied:
                    return "El servidor rechazó el usuario o la contraseña de la conexión.";
                case MySqlErrorCode.UnknownDatabase:
                    return "La base de datos indicada no existe en el servidor.";
                default:
                    return "Error al conectar con la base de datos: " + ex.Message;
            }
        }
    }
}
