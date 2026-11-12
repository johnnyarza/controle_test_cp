using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using ProbetaControl.Models;

namespace ProbetaControl.Data
{
    /// <summary>
    /// Concrete designs and their 8 material slots. Slots are never deleted: an emptied slot keeps
    /// its row with MaterialId and QuantityKgPerM3 set to NULL.
    /// </summary>
    public sealed class ConcreteDesignRepository : RepositoryBase
    {
        private const string Table = "ConcreteDesigns";
        private const string DuplicateMessage = "Ya existe una dosificación con esa descripción.";
        private const string DesignColumns = "Id, Description, Fck, Slump, IsArchived, RowVersion";

        private const string SlotSelect =
            "SELECT s.ConcreteDesignId, s.Position, s.MaterialId, s.QuantityKgPerM3, m.Name AS MaterialName, m.MaterialType, p.Name AS ProviderName " +
            "FROM ConcreteDesignMaterials s " +
            "JOIN Materials m ON m.Id = s.MaterialId " +
            "LEFT JOIN Providers p ON p.Id = m.ProviderId ";

        public ConcreteDesignRepository(DbConnectionFactory db, IAuditContext audit)
            : base(db, audit)
        {
        }

        /// <summary>One page with materials, ordered by description.</summary>
        public async Task<PagedResult<ConcreteDesign>> SearchAsync(ListQuery query, CancellationToken cancellationToken = default)
        {
            const string where = "WHERE (@includeArchived OR IsArchived = 0) AND (@pattern IS NULL OR Description LIKE @pattern) ";
            var sql = $"SELECT COUNT(*) FROM {Table} {where}; " +
                      $"SELECT {DesignColumns} FROM {Table} {where} ORDER BY Description LIMIT @limit OFFSET @offset";

            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                int total;
                List<ConcreteDesign> designs;
                using (var results = await connection.QueryMultipleAsync(Cmd(sql, new
                {
                    includeArchived = query.IncludeArchived,
                    pattern = Contains(query.Text),
                    limit = query.Page.Limit,
                    offset = query.Page.Offset,
                }, null, cancellationToken)).ConfigureAwait(false))
                {
                    total = await results.ReadSingleAsync<int>().ConfigureAwait(false);
                    designs = (await results.ReadAsync<ConcreteDesign>().ConfigureAwait(false)).ToList();
                }

                await LoadMaterialsAsync(connection, null, designs, cancellationToken).ConfigureAwait(false);
                return new PagedResult<ConcreteDesign>(designs, total, query.Page);
            }
        }

        public async Task<ConcreteDesign> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await GetAsync(connection, null, id, false, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task InsertAsync(ConcreteDesign design, CancellationToken cancellationToken = default)
        {
            return WithUniqueCheckAsync(() => InTransactionAsync(async (connection, transaction) =>
            {
                var id = await connection.ExecuteScalarAsync<int>(Cmd(
                    $"INSERT INTO {Table} (Description, Fck, Slump) VALUES (@Description, @Fck, @Slump); SELECT LAST_INSERT_ID();",
                    new { Description = Clean(design.Description), design.Fck, design.Slump }, transaction, cancellationToken)).ConfigureAwait(false);

                design.Id = id;
                design.RowVersion = 1;
                await WriteSlotsAsync(connection, transaction, design, cancellationToken).ConfigureAwait(false);
                await AuditWriter.InsertedAsync(connection, transaction, Audit, Table, id, design, cancellationToken).ConfigureAwait(false);
                return id;
            }, cancellationToken), "Description", DuplicateMessage);
        }

        public Task UpdateAsync(ConcreteDesign design, CancellationToken cancellationToken = default)
        {
            return WithUniqueCheckAsync(() => InTransactionAsync(async (connection, transaction) =>
            {
                var before = await GetAsync(connection, transaction, design.Id, true, cancellationToken).ConfigureAwait(false);

                var rows = await connection.ExecuteAsync(Cmd(
                    $"UPDATE {Table} SET Description = @Description, Fck = @Fck, Slump = @Slump, RowVersion = RowVersion + 1 " +
                    "WHERE Id = @Id AND RowVersion = @RowVersion",
                    new { design.Id, Description = Clean(design.Description), design.Fck, design.Slump, design.RowVersion },
                    transaction, cancellationToken)).ConfigureAwait(false);
                await EnsureUpdatedAsync(connection, transaction, Table, design.Id, rows, cancellationToken).ConfigureAwait(false);

                await WriteSlotsAsync(connection, transaction, design, cancellationToken).ConfigureAwait(false);
                await AuditWriter.UpdatedAsync(connection, transaction, Audit, Table, design.Id, before, design, cancellationToken).ConfigureAwait(false);
                design.RowVersion++;
                return true;
            }, cancellationToken), "Description", DuplicateMessage);
        }

        /// <summary>Active tests made with the design, newest first.</summary>
        public Task<RecordUsage> GetUsageAsync(int id, CancellationToken cancellationToken = default) => GetUsageAsync(
            "FROM CompressionTests t WHERE t.IsArchived = 0 AND t.ConcreteDesignId = @id",
            TestNameExpression, "t.TestDate DESC, t.Id DESC", id, cancellationToken);

        /// <summary>All tests made with the design, archived ones included (their reports show the design too).</summary>
        public async Task<int> CountTestsAsync(int id, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await connection.ExecuteScalarAsync<int>(Cmd(
                    "SELECT COUNT(*) FROM CompressionTests WHERE ConcreteDesignId = @id", new { id }, null, cancellationToken)).ConfigureAwait(false);
            }
        }

        public async Task SetArchivedAsync(ConcreteDesign design, bool archived, CancellationToken cancellationToken = default)
        {
            design.RowVersion = await SetArchivedAsync(Table, design.Id, design.RowVersion, archived, cancellationToken).ConfigureAwait(false);
            design.IsArchived = archived;
        }

        private static async Task<ConcreteDesign> GetAsync(MySqlConnection connection, MySqlTransaction transaction, int id, bool forUpdate,
            CancellationToken cancellationToken)
        {
            var design = await connection.QuerySingleOrDefaultAsync<ConcreteDesign>(Cmd(
                $"SELECT {DesignColumns} FROM {Table} WHERE Id = @id" + (forUpdate ? " FOR UPDATE" : string.Empty),
                new { id }, transaction, cancellationToken)).ConfigureAwait(false);

            if (design != null)
            {
                await LoadMaterialsAsync(connection, transaction, new[] { design }, cancellationToken).ConfigureAwait(false);
            }

            return design;
        }

        private static async Task LoadMaterialsAsync(MySqlConnection connection, MySqlTransaction transaction,
            IReadOnlyCollection<ConcreteDesign> designs, CancellationToken cancellationToken)
        {
            if (designs.Count == 0)
            {
                return;
            }

            var slots = await connection.QueryAsync<SlotRow>(Cmd(
                SlotSelect + "WHERE s.ConcreteDesignId IN @ids AND s.MaterialId IS NOT NULL ORDER BY s.ConcreteDesignId, s.Position",
                new { ids = designs.Select(d => d.Id).ToArray() }, transaction, cancellationToken)).ConfigureAwait(false);

            var byDesign = slots.ToLookup(s => s.ConcreteDesignId);
            foreach (var design in designs)
            {
                design.Materials = byDesign[design.Id].Select(s => new ConcreteDesignMaterial
                {
                    Position = s.Position,
                    MaterialId = s.MaterialId,
                    QuantityKgPerM3 = s.QuantityKgPerM3,
                    MaterialName = s.MaterialName,
                    ProviderName = s.ProviderName,
                    MaterialType = s.MaterialType,
                }).ToList();
            }
        }

        /// <summary>Writes all 8 slots: filled ones from the design, the rest emptied (never deleted).</summary>
        private static async Task WriteSlotsAsync(MySqlConnection connection, MySqlTransaction transaction, ConcreteDesign design,
            CancellationToken cancellationToken)
        {
            const string upsert =
                "INSERT INTO ConcreteDesignMaterials (ConcreteDesignId, Position, MaterialId, QuantityKgPerM3) " +
                "VALUES (@designId, @Position, @MaterialId, @QuantityKgPerM3) AS incoming " +
                "ON DUPLICATE KEY UPDATE MaterialId = incoming.MaterialId, QuantityKgPerM3 = incoming.QuantityKgPerM3";

            var slots = Enumerable.Range(1, ConcreteDesign.MaxMaterials).Select(position =>
            {
                var slot = design.Materials.FirstOrDefault(m => m.Position == position && m.MaterialId.HasValue);
                return new
                {
                    designId = design.Id,
                    Position = position,
                    MaterialId = slot?.MaterialId,
                    QuantityKgPerM3 = slot?.MaterialId == null ? null : slot.QuantityKgPerM3,
                };
            });

            await connection.ExecuteAsync(Cmd(upsert, slots, transaction, cancellationToken)).ConfigureAwait(false);
        }

        private sealed class SlotRow
        {
            public int ConcreteDesignId { get; set; }

            public int Position { get; set; }

            public int? MaterialId { get; set; }

            public decimal? QuantityKgPerM3 { get; set; }

            public string MaterialName { get; set; }

            public string ProviderName { get; set; }

            public string MaterialType { get; set; }
        }
    }
}
