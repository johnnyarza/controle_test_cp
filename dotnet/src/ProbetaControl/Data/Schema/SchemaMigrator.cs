using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace ProbetaControl.Data.Schema
{
    /// <summary>
    /// Reads the schema version of a database and applies the pending numbered scripts.
    /// Applying needs an account with DDL rights (specimen_admin); the app's own account
    /// (SELECT/INSERT/UPDATE) can only read the version.
    /// </summary>
    /// <remarks>
    /// MySQL DDL is not transactional: if a script fails half-way, fix the cause, undo what the
    /// script created, and run again. Keep scripts small and free of DELIMITER blocks.
    /// </remarks>
    public sealed class SchemaMigrator
    {
        private readonly string _connectionString;
        private readonly IReadOnlyList<SchemaScript> _scripts;

        /// <param name="connectionString">Connection string that includes the target database.</param>
        public SchemaMigrator(string connectionString)
            : this(connectionString, SchemaScript.LoadEmbedded())
        {
        }

        internal SchemaMigrator(string connectionString, IReadOnlyList<SchemaScript> scripts)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _scripts = scripts;
        }

        /// <summary>Highest applied version, or 0 for an empty database.</summary>
        public async Task<int> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return await GetCurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>Applies every script newer than the current version. Returns the scripts applied.</summary>
        public async Task<IReadOnlyList<SchemaScript>> MigrateAsync(
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            var applied = new List<SchemaScript>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var current = await GetCurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);
                var latest = _scripts.Count == 0 ? 0 : _scripts.Max(s => s.Version);
                if (current > latest)
                {
                    throw new SchemaMigrationException(
                        $"La base de datos está en la versión {current}, más nueva que este programa ({latest}). Actualice el programa.");
                }

                foreach (var script in _scripts.Where(s => s.Version > current))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report($"Aplicando {script.FileName}…");

                    try
                    {
                        using (var command = new MySqlCommand(script.Sql, connection))
                        {
                            command.CommandTimeout = 600;
                            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (MySqlException ex)
                    {
                        throw new SchemaMigrationException($"Error al aplicar {script.FileName}: {ex.Message}", ex);
                    }

                    var after = await GetCurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);
                    if (after != script.Version)
                    {
                        throw new SchemaMigrationException(
                            $"{script.FileName} terminó sin registrar la versión {script.Version} en SchemaVersion.");
                    }

                    applied.Add(script);
                }
            }

            return applied;
        }

        private static async Task<int> GetCurrentVersionAsync(MySqlConnection connection, CancellationToken cancellationToken)
        {
            // Table names are stored in lower case on Windows servers, so compare case-insensitively.
            const string tableExistsSql =
                "SELECT COUNT(*) FROM information_schema.TABLES " +
                "WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'schemaversion'";

            using (var command = new MySqlCommand(tableExistsSql, connection))
            {
                var exists = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
                if (exists == 0)
                {
                    return 0;
                }
            }

            using (var command = new MySqlCommand("SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion", connection))
            {
                return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            }
        }
    }

    public sealed class SchemaMigrationException : Exception
    {
        public SchemaMigrationException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
