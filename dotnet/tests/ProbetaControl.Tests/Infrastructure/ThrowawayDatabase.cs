using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MySqlConnector;
using ProbetaControl.Data.Schema;
using ProbetaControl.Services;

namespace ProbetaControl.Tests.Infrastructure
{
    /// <summary>
    /// A probeta_test_* database that exists for one test fixture: created and migrated with the
    /// test admin account, used through the restricted app account, dropped at the end.
    /// </summary>
    public sealed class ThrowawayDatabase
    {
        private const string Prefix = "probeta_test_";
        private const string StampFormat = "yyyyMMddHHmmss";

        private readonly TestServer _server;
        private readonly TestSettings _settings;

        private ThrowawayDatabase(TestServer server, TestSettings settings, string name)
        {
            _server = server;
            _settings = settings;
            Name = name;
        }

        public string Name { get; }

        public IReadOnlyList<SchemaScript> AppliedScripts { get; private set; }

        /// <summary>Connection string for the test admin account, on this database.</summary>
        public string AdminConnectionString => Build(_settings.AdminUser, _settings.AdminPassword, Name);

        /// <summary>Connection string for the restricted app account (SELECT, INSERT, UPDATE).</summary>
        public string AppConnectionString => Build(_settings.AppUser, _settings.AppPassword, Name);

        /// <summary>The restricted app account as the app itself would configure it.</summary>
        public ConnectionSettings AppConnectionSettings => new ConnectionSettings
        {
            Host = _server.Host,
            Port = _server.Port,
            Database = Name,
            UserName = _settings.AppUser,
            Password = _settings.AppPassword,
        };

        public static async Task<ThrowawayDatabase> CreateAsync(TestServer server, TestSettings settings)
        {
            var stamp = DateTime.UtcNow.ToString(StampFormat, CultureInfo.InvariantCulture);
            var name = Prefix + stamp + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            var database = new ThrowawayDatabase(server, settings, name);

            await database.DropStaleDatabasesAsync();
            await database.ExecuteAsServerAdminAsync(
                $"CREATE DATABASE `{name}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci");

            try
            {
                // The app test account already has SELECT/INSERT/UPDATE on probeta_test_* (setup script).
                database.AppliedScripts = await new SchemaMigrator(database.AdminConnectionString).MigrateAsync();
            }
            catch
            {
                await database.DropAsync();
                throw;
            }

            return database;
        }

        public Task DropAsync() => ExecuteAsServerAdminAsync($"DROP DATABASE IF EXISTS `{Name}`");

        /// <summary>Removes databases left behind by test runs that crashed more than a day ago.</summary>
        private async Task DropStaleDatabasesAsync()
        {
            var stale = new List<string>();
            using (var connection = new MySqlConnection(Build(_settings.AdminUser, _settings.AdminPassword, null)))
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand("SHOW DATABASES LIKE 'probeta\\_test\\_%'", connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var existing = reader.GetString(0);
                        if (existing.Length >= Prefix.Length + StampFormat.Length &&
                            DateTime.TryParseExact(existing.Substring(Prefix.Length, StampFormat.Length), StampFormat,
                                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                out var created) &&
                            created < DateTime.UtcNow.AddDays(-1))
                        {
                            stale.Add(existing);
                        }
                    }
                }
            }

            foreach (var name in stale)
            {
                await ExecuteAsServerAdminAsync($"DROP DATABASE IF EXISTS `{name}`");
            }
        }

        private async Task ExecuteAsServerAdminAsync(string sql)
        {
            using (var connection = new MySqlConnection(Build(_settings.AdminUser, _settings.AdminPassword, null)))
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(sql, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private string Build(string user, string password, string database)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = _server.Host,
                Port = _server.Port,
                UserID = user,
                Password = password ?? string.Empty,
                Pooling = false,
                ConnectionTimeout = 10,
            };

            if (database != null)
            {
                builder.Database = database;
            }

            return builder.ConnectionString;
        }
    }
}
