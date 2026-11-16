using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using ProbetaControl.Data;
using ProbetaControl.Data.Schema;
using ProbetaControl.Models;
using ProbetaControl.Services;

namespace ProbetaControl.LegacyImport
{
    public sealed class ImportException : Exception
    {
        public ImportException(string message, Exception inner = null)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Writes an <see cref="ImportPlan"/> into an empty new database with the original numbers, in one transaction,
    /// with an audit row per record (machine "IMPORTACION cp_db", user admin). Needs an account with structure rights
    /// (specimen_admin): the next numbers are set with ALTER TABLE before the data goes in, so a new record never gets
    /// a legacy number, deleted ones included.
    /// </summary>
    public static class ImportWriter
    {
        public const string MachineName = "IMPORTACION cp_db";

        private static readonly string[] DataTables = { "Clients", "Providers", "Materials", "ConcreteDesigns", "CompressionTests", "Specimens" };

        /// <param name="commit">False = check only: everything is written and checked, then rolled back.</param>
        public static async Task WriteAsync(string targetConnectionString, ImportPlan plan, bool commit)
        {
            using (var connection = new MySqlConnection(targetConnectionString))
            {
                await connection.OpenAsync();
                await CheckTargetAsync(connection, targetConnectionString);

                var admin = await connection.QuerySingleOrDefaultAsync<User>("SELECT Id, UserName, Role FROM Users WHERE UserName = 'admin'")
                    ?? throw new ImportException("La base de datos nueva no tiene el usuario admin.");
                var audit = new AppSession(MachineName);
                audit.SignIn(admin);

                // ALTER commits on its own, so it runs first, on the still empty tables. Explicit numbers inserted
                // later never lower the counter. A check run leaves the counters alone.
                if (commit)
                {
                    foreach (var pair in plan.NextIds)
                    {
                        await connection.ExecuteAsync($"ALTER TABLE {pair.Key} AUTO_INCREMENT = {pair.Value}");
                    }
                }

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    await InsertAllAsync(connection, transaction, plan, audit);
                    await VerifyAsync(connection, transaction, plan);
                    if (commit)
                    {
                        await transaction.CommitAsync();
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                    }
                }

                if (commit)
                {
                    await VerifyNextIdsAsync(connection, plan);
                }
            }
        }

        private static async Task CheckTargetAsync(MySqlConnection connection, string connectionString)
        {
            var version = await new SchemaMigrator(connectionString).GetCurrentVersionAsync();
            if (version != SchemaInfo.RequiredVersion)
            {
                throw new ImportException($"La base de datos nueva está en la versión {version}; esta herramienta necesita la " +
                    $"{SchemaInfo.RequiredVersion}. Actualícela primero con el programa (\"Actualizar base de datos\").");
            }

            foreach (var table in DataTables)
            {
                if (await connection.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM {table}") > 0)
                {
                    throw new ImportException($"La base de datos nueva ya tiene datos ({table}). La importación solo se hace " +
                        "una vez, en una base de datos vacía.");
                }
            }
        }

        private static async Task InsertAllAsync(MySqlConnection c, MySqlTransaction t, ImportPlan plan, AppSession audit)
        {
            await InsertAsync(c, t, audit, "Clients", plan.Clients, r => r.Id,
                "INSERT INTO Clients (Id, Name, Phone, Address, Email) VALUES (@Id, @Name, @Phone, @Address, @Email)");
            await InsertAsync(c, t, audit, "Providers", plan.Providers, r => r.Id,
                "INSERT INTO Providers (Id, Name, Phone, Address, Email) VALUES (@Id, @Name, @Phone, @Address, @Email)");
            await InsertAsync(c, t, audit, "Materials", plan.Materials, r => r.Id,
                "INSERT INTO Materials (Id, Name, MaterialType, ProviderId) VALUES (@Id, @Name, @MaterialType, @ProviderId)");
            await InsertAsync(c, t, audit, "ConcreteDesigns", plan.Designs, r => r.Id,
                "INSERT INTO ConcreteDesigns (Id, Description, Fck, Slump) VALUES (@Id, @Description, @Fck, @Slump)");
            var rows = plan.Designs.SelectMany(d => d.Materials.Select(m => new { DesignId = d.Id, m.Position, m.MaterialId, m.QuantityKgPerM3 }));
            await c.ExecuteAsync(
                "INSERT INTO ConcreteDesignMaterials (ConcreteDesignId, Position, MaterialId, QuantityKgPerM3) " +
                "VALUES (@DesignId, @Position, @MaterialId, @QuantityKgPerM3)", rows, t);
            await InsertAsync(c, t, audit, "CompressionTests", plan.Tests, r => r.Id,
                "INSERT INTO CompressionTests (Id, ClientId, ConcreteProviderId, ConcreteDesignId, ProjectName, Address, TestDate) " +
                "VALUES (@Id, @ClientId, @ConcreteProviderId, @ConcreteDesignId, @ProjectName, @Address, @TestDate)");
            await InsertAsync(c, t, audit, "Specimens", plan.Specimens, r => r.Id,
                "INSERT INTO Specimens (Id, CompressionTestId, PublicCode, Code, Slump, MoldDate, RuptureDate, DiameterCm, HeightCm, " +
                "WeightKg, RuptureLoadTon, IsLocked) VALUES (@Id, @CompressionTestId, @PublicCode, @Code, @Slump, @MoldDate, @RuptureDate, " +
                "@DiameterCm, @HeightCm, @WeightKg, @RuptureLoadTon, @IsLocked)");
        }

        private static async Task InsertAsync<T>(MySqlConnection c, MySqlTransaction t, AppSession audit, string table,
            IEnumerable<T> records, Func<T, int> id, string sql)
            where T : IAuditable
        {
            foreach (var record in records)
            {
                try
                {
                    await c.ExecuteAsync(sql, record, t);
                    await AuditWriter.InsertedAsync(c, t, audit, table, id(record), record, CancellationToken.None);
                }
                catch (MySqlException ex)
                {
                    throw new ImportException($"No se pudo importar {table} Nº {id(record)}: {ex.Message}", ex);
                }
            }
        }

        /// <summary>Every planned number is in the new database (still inside the transaction).</summary>
        private static async Task VerifyAsync(MySqlConnection c, MySqlTransaction t, ImportPlan plan)
        {
            async Task Check(string table, IEnumerable<int> expected)
            {
                var ids = new HashSet<int>(await c.QueryAsync<int>($"SELECT Id FROM {table}", transaction: t));
                var missing = expected.Where(i => !ids.Contains(i)).ToList();
                if (missing.Count > 0 || ids.Count != expected.Count())
                {
                    throw new ImportException($"Comprobación fallida en {table}: faltan los Nº {string.Join(", ", missing.Take(20))}. No se guardó nada.");
                }
            }

            await Check("Clients", plan.Clients.Select(r => r.Id));
            await Check("Providers", plan.Providers.Select(r => r.Id));
            await Check("Materials", plan.Materials.Select(r => r.Id));
            await Check("ConcreteDesigns", plan.Designs.Select(r => r.Id));
            await Check("CompressionTests", plan.Tests.Select(r => r.Id));
            await Check("Specimens", plan.Specimens.Select(r => r.Id));
        }

        /// <summary>After the commit: each table will give its next record a number after every legacy one.</summary>
        private static async Task VerifyNextIdsAsync(MySqlConnection c, ImportPlan plan)
        {
            await c.ExecuteAsync("SET SESSION information_schema_stats_expiry = 0");
            foreach (var pair in plan.NextIds)
            {
                var next = await c.ExecuteScalarAsync<long?>(
                    "SELECT AUTO_INCREMENT FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table",
                    new { table = pair.Key });
                if (next == null || next < pair.Value)
                {
                    throw new ImportException($"Los datos se importaron, pero {pair.Key} daría el Nº {next} a un registro nuevo " +
                        $"(debe ser {pair.Value} o más). Ejecute: ALTER TABLE {pair.Key} AUTO_INCREMENT = {pair.Value};");
                }
            }
        }
    }
}
