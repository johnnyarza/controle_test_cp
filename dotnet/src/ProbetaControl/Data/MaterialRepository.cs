using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ProbetaControl.Models;

namespace ProbetaControl.Data
{
    public sealed class MaterialRepository : RepositoryBase
    {
        private const string Select =
            "SELECT m.Id, m.Name, m.MaterialType, m.ProviderId, p.Name AS ProviderName, m.IsArchived, m.RowVersion " +
            "FROM Materials m LEFT JOIN Providers p ON p.Id = m.ProviderId ";

        public MaterialRepository(DbConnectionFactory db, IAuditContext audit)
            : base(db, audit)
        {
        }

        /// <summary>One page, ordered by name; Text matches material or provider name.</summary>
        public async Task<PagedResult<Material>> SearchAsync(ListQuery query, CancellationToken cancellationToken = default)
        {
            const string where = "WHERE (@includeArchived OR m.IsArchived = 0) AND (@pattern IS NULL OR m.Name LIKE @pattern OR p.Name LIKE @pattern) ";
            var sql = "SELECT COUNT(*) FROM Materials m LEFT JOIN Providers p ON p.Id = m.ProviderId " + where + "; " +
                      Select + where + "ORDER BY m.Name LIMIT @limit OFFSET @offset";

            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            using (var results = await connection.QueryMultipleAsync(Cmd(sql, new
            {
                includeArchived = query.IncludeArchived,
                pattern = Contains(query.Text),
                limit = query.Page.Limit,
                offset = query.Page.Offset,
            }, null, cancellationToken)).ConfigureAwait(false))
            {
                var total = await results.ReadSingleAsync<int>().ConfigureAwait(false);
                var items = (await results.ReadAsync<Material>().ConfigureAwait(false)).ToList();
                return new PagedResult<Material>(items, total, query.Page);
            }
        }

        public async Task<Material> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await connection.QuerySingleOrDefaultAsync<Material>(
                    Cmd(Select + "WHERE m.Id = @id", new { id }, null, cancellationToken)).ConfigureAwait(false);
            }
        }

        public Task InsertAsync(Material material, CancellationToken cancellationToken = default)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                var id = await connection.ExecuteScalarAsync<int>(Cmd(
                    "INSERT INTO Materials (Name, MaterialType, ProviderId) VALUES (@Name, @MaterialType, @ProviderId); SELECT LAST_INSERT_ID();",
                    new { Name = Clean(material.Name), material.MaterialType, material.ProviderId }, transaction, cancellationToken)).ConfigureAwait(false);

                material.Id = id;
                material.RowVersion = 1;
                await AuditWriter.InsertedAsync(connection, transaction, Audit, "Materials", id, material, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public Task UpdateAsync(Material material, CancellationToken cancellationToken = default)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                var before = await connection.QuerySingleOrDefaultAsync<Material>(Cmd(
                    "SELECT Id, Name, MaterialType, ProviderId FROM Materials WHERE Id = @Id FOR UPDATE", new { material.Id }, transaction, cancellationToken)).ConfigureAwait(false);

                var rows = await connection.ExecuteAsync(Cmd(
                    "UPDATE Materials SET Name = @Name, MaterialType = @MaterialType, ProviderId = @ProviderId, RowVersion = RowVersion + 1 WHERE Id = @Id AND RowVersion = @RowVersion",
                    new { material.Id, Name = Clean(material.Name), material.MaterialType, material.ProviderId, material.RowVersion }, transaction, cancellationToken)).ConfigureAwait(false);
                await EnsureUpdatedAsync(connection, transaction, "Materials", material.Id, rows, cancellationToken).ConfigureAwait(false);

                await AuditWriter.UpdatedAsync(connection, transaction, Audit, "Materials", material.Id, before, material, cancellationToken).ConfigureAwait(false);
                material.RowVersion++;
            }, cancellationToken);
        }

        /// <summary>Active designs that use the material.</summary>
        public Task<RecordUsage> GetUsageAsync(int id, CancellationToken cancellationToken = default) => GetUsageAsync(
            "FROM ConcreteDesigns d WHERE d.IsArchived = 0 AND EXISTS " +
            "(SELECT 1 FROM ConcreteDesignMaterials s WHERE s.ConcreteDesignId = d.Id AND s.MaterialId = @id)",
            "d.Description", "d.Description", id, cancellationToken);

        public async Task SetArchivedAsync(Material material, bool archived, CancellationToken cancellationToken = default)
        {
            material.RowVersion = await SetArchivedAsync("Materials", material.Id, material.RowVersion, archived, cancellationToken).ConfigureAwait(false);
            material.IsArchived = archived;
        }
    }
}
