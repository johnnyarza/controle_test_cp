using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;

namespace ProbetaControl.LegacyImport
{
    /// <summary>A client or provider of cp_db (same columns).</summary>
    public sealed class LegacyContact
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public string Email { get; set; }
    }

    public sealed class LegacyMaterial
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? ProviderId { get; set; }
    }

    /// <summary>cp_db.concretedesign: up to 8 material slots (matN_id, matN_qtt).</summary>
    public sealed class LegacyDesign
    {
        public int Id { get; set; }

        public string Description { get; set; }

        public double? Fck { get; set; }

        public double? Slump { get; set; }

        public int?[] MaterialIds { get; set; } = new int?[8];

        public double?[] Quantities { get; set; } = new double?[8];
    }

    /// <summary>cp_db.compresion_test (an ensayo).</summary>
    public sealed class LegacyTest
    {
        public int Id { get; set; }

        public int? ClientId { get; set; }

        public int? ConcreteProviderId { get; set; }

        public int? DesignId { get; set; }

        public string Obra { get; set; }

        public string Address { get; set; }

        public DateTime? Date { get; set; }
    }

    /// <summary>cp_db.corpo_de_provas (a probeta). A load of 0 meant "not broken".</summary>
    public sealed class LegacySpecimen
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public int? TestId { get; set; }

        public decimal? Slump { get; set; }

        public DateTime? MoldDate { get; set; }

        public DateTime? RuptureDate { get; set; }

        public int? Age { get; set; }

        public decimal? Diameter { get; set; }

        public decimal? Height { get; set; }

        public decimal? Weight { get; set; }

        public decimal? Load { get; set; }

        public bool IsLocked { get; set; }
    }

    /// <summary>Everything read from cp_db.</summary>
    public sealed class LegacyData
    {
        public List<LegacyContact> Clients { get; set; } = new List<LegacyContact>();

        public List<LegacyContact> Providers { get; set; } = new List<LegacyContact>();

        public List<LegacyMaterial> Materials { get; set; } = new List<LegacyMaterial>();

        public List<LegacyDesign> Designs { get; set; } = new List<LegacyDesign>();

        public List<LegacyTest> Tests { get; set; } = new List<LegacyTest>();

        public List<LegacySpecimen> Specimens { get; set; } = new List<LegacySpecimen>();

        /// <summary>
        /// The number each legacy table would give its next row (its AUTO_INCREMENT), by legacy table name. Numbers of
        /// deleted rows are below it, so new numbering starts here and never repeats an old number.
        /// </summary>
        public Dictionary<string, long> NextIds { get; set; } = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Reads cp_db in a read-only session: even with root, MySQL refuses any write to it.</summary>
    public static class LegacyReader
    {
        public static async Task<LegacyData> ReadAsync(string connectionString)
        {
            // Legacy dates may be 0000-00-00: read them as DateTime.MinValue and treat them as missing.
            var builder = new MySqlConnectionStringBuilder(connectionString) { ConvertZeroDateTime = true };
            using (var connection = new MySqlConnection(builder.ConnectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync("SET SESSION TRANSACTION READ ONLY");
                try
                {
                    // MySQL 8 caches AUTO_INCREMENT in information_schema for a day; read the current value.
                    await connection.ExecuteAsync("SET SESSION information_schema_stats_expiry = 0");
                }
                catch (MySqlException)
                {
                    // Older servers have no cache (and no such variable).
                }

                var data = new LegacyData
                {
                    Clients = (await connection.QueryAsync<LegacyContact>(
                        "SELECT id AS Id, name AS Name, phone AS Phone, address AS Address, email AS Email FROM clients ORDER BY id")).ToList(),
                    Providers = (await connection.QueryAsync<LegacyContact>(
                        "SELECT id AS Id, name AS Name, phone AS Phone, address AS Address, email AS Email FROM providers ORDER BY id")).ToList(),
                    Materials = (await connection.QueryAsync<LegacyMaterial>(
                        "SELECT id AS Id, name AS Name, providerId AS ProviderId FROM materials ORDER BY id")).ToList(),
                    Tests = (await connection.QueryAsync<LegacyTest>(
                        "SELECT id AS Id, client_id AS ClientId, concreteProviderId AS ConcreteProviderId, ConcreteDesign_id AS DesignId, " +
                        "obra AS Obra, address AS Address, creacionDate AS Date FROM compresion_test ORDER BY id")).ToList(),
                };

                var designRows = (IEnumerable<object>)await connection.QueryAsync("SELECT * FROM concretedesign ORDER BY id");
                foreach (var row in designRows.Cast<IDictionary<string, object>>())
                {
                    var design = new LegacyDesign
                    {
                        Id = Convert.ToInt32(Get(row, "id")),
                        Description = Get(row, "description") as string,
                        Fck = ToDouble(Get(row, "fck")),
                        Slump = ToDouble(Get(row, "slump")),
                    };
                    for (var slot = 0; slot < 8; slot++)
                    {
                        var id = Get(row, $"mat{slot + 1}_id");
                        design.MaterialIds[slot] = id == null ? (int?)null : Convert.ToInt32(id);
                        design.Quantities[slot] = ToDouble(Get(row, $"mat{slot + 1}_qtt"));
                    }

                    data.Designs.Add(design);
                }

                // is_locked was added by a later legacy version; a database without it has nothing locked.
                var hasLocked = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() " +
                    "AND TABLE_NAME = 'corpo_de_provas' AND COLUMN_NAME = 'is_locked'") > 0;
                data.Specimens = (await connection.QueryAsync<LegacySpecimen>(
                    "SELECT id AS Id, code AS Code, compresionTest_Id AS TestId, slump AS Slump, dateMolde AS MoldDate, " +
                    "ruptureDate AS RuptureDate, age AS Age, diameter AS Diameter, height AS Height, weight AS Weight, " +
                    "tonRupture AS `Load`, " + (hasLocked ? "is_locked <> 0" : "FALSE") + " AS IsLocked FROM corpo_de_provas ORDER BY id")).ToList();

                foreach (var specimen in data.Specimens)
                {
                    specimen.MoldDate = Real(specimen.MoldDate);
                    specimen.RuptureDate = Real(specimen.RuptureDate);
                }

                foreach (var test in data.Tests)
                {
                    test.Date = Real(test.Date);
                }

                var next = await connection.QueryAsync<(string Table, long? Next)>(
                    "SELECT TABLE_NAME, AUTO_INCREMENT FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE()");
                foreach (var (table, value) in next)
                {
                    data.NextIds[table] = value ?? 1;
                }

                return data;
            }
        }

        private static object Get(IDictionary<string, object> row, string column)
        {
            var key = row.Keys.FirstOrDefault(k => string.Equals(k, column, StringComparison.OrdinalIgnoreCase));
            var value = key == null ? null : row[key];
            return value is DBNull ? null : value;
        }

        private static double? ToDouble(object value) => value == null ? (double?)null : Convert.ToDouble(value);

        private static DateTime? Real(DateTime? value) => value == null || value.Value == DateTime.MinValue ? null : value;
    }
}
