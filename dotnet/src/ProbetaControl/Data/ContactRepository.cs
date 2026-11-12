using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ProbetaControl.Models;

namespace ProbetaControl.Data
{
    /// <summary>Clients and Providers have the same columns; one implementation serves both.</summary>
    public abstract class ContactRepository<T> : RepositoryBase
        where T : ContactRecord
    {
        private const string Columns = "Id, Name, Phone, Address, Email, IsArchived, RowVersion";

        private readonly string _table;
        private readonly string _duplicateMessage;

        protected ContactRepository(DbConnectionFactory db, IAuditContext audit, string table, string duplicateMessage)
            : base(db, audit)
        {
            _table = table;
            _duplicateMessage = duplicateMessage;
        }

        /// <summary>One page, ordered by name; Text matches name, phone or email.</summary>
        public async Task<PagedResult<T>> SearchAsync(ListQuery query, CancellationToken cancellationToken = default)
        {
            var where = "WHERE (@includeArchived OR IsArchived = 0) " +
                        "AND (@pattern IS NULL OR Name LIKE @pattern OR Phone LIKE @pattern OR Email LIKE @pattern)";
            var sql = $"SELECT COUNT(*) FROM {_table} {where}; " +
                      $"SELECT {Columns} FROM {_table} {where} ORDER BY Name LIMIT @limit OFFSET @offset";
            var parameters = new
            {
                includeArchived = query.IncludeArchived,
                pattern = Contains(query.Text),
                limit = query.Page.Limit,
                offset = query.Page.Offset,
            };

            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            using (var results = await connection.QueryMultipleAsync(Cmd(sql, parameters, null, cancellationToken)).ConfigureAwait(false))
            {
                var total = await results.ReadSingleAsync<int>().ConfigureAwait(false);
                var items = (await results.ReadAsync<T>().ConfigureAwait(false)).ToList();
                return new PagedResult<T>(items, total, query.Page);
            }
        }

        public async Task<T> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await connection.QuerySingleOrDefaultAsync<T>(
                    Cmd($"SELECT {Columns} FROM {_table} WHERE Id = @id", new { id }, null, cancellationToken)).ConfigureAwait(false);
            }
        }

        /// <summary>Inserts and sets Id and RowVersion on <paramref name="record"/>.</summary>
        public Task InsertAsync(T record, CancellationToken cancellationToken = default)
        {
            return WithUniqueCheckAsync(() => InTransactionAsync(async (connection, transaction) =>
            {
                var id = await connection.ExecuteScalarAsync<int>(Cmd(
                    $"INSERT INTO {_table} (Name, Phone, Address, Email) VALUES (@Name, @Phone, @Address, @Email); SELECT LAST_INSERT_ID();",
                    Parameters(record), transaction, cancellationToken)).ConfigureAwait(false);

                record.Id = id;
                record.RowVersion = 1;
                await AuditWriter.InsertedAsync(connection, transaction, Audit, _table, id, record, cancellationToken).ConfigureAwait(false);
                return id;
            }, cancellationToken), "Name", _duplicateMessage);
        }

        /// <summary>Saves if nobody changed the row since it was read; bumps RowVersion on <paramref name="record"/>.</summary>
        public Task UpdateAsync(T record, CancellationToken cancellationToken = default)
        {
            return WithUniqueCheckAsync(() => InTransactionAsync(async (connection, transaction) =>
            {
                var before = await connection.QuerySingleOrDefaultAsync<T>(Cmd(
                    $"SELECT {Columns} FROM {_table} WHERE Id = @Id FOR UPDATE", new { record.Id }, transaction, cancellationToken)).ConfigureAwait(false);

                var rows = await connection.ExecuteAsync(Cmd(
                    $"UPDATE {_table} SET Name = @Name, Phone = @Phone, Address = @Address, Email = @Email, RowVersion = RowVersion + 1 " +
                    "WHERE Id = @Id AND RowVersion = @RowVersion",
                    Parameters(record), transaction, cancellationToken)).ConfigureAwait(false);
                await EnsureUpdatedAsync(connection, transaction, _table, record.Id, rows, cancellationToken).ConfigureAwait(false);

                await AuditWriter.UpdatedAsync(connection, transaction, Audit, _table, record.Id, before, record, cancellationToken).ConfigureAwait(false);
                record.RowVersion++;
                return true;
            }, cancellationToken), "Name", _duplicateMessage);
        }

        public async Task SetArchivedAsync(T record, bool archived, CancellationToken cancellationToken = default)
        {
            record.RowVersion = await SetArchivedAsync(_table, record.Id, record.RowVersion, archived, cancellationToken).ConfigureAwait(false);
            record.IsArchived = archived;
        }

        private static object Parameters(T record) => new
        {
            record.Id,
            Name = Clean(record.Name),
            Phone = Clean(record.Phone),
            Address = Clean(record.Address),
            Email = Clean(record.Email),
            record.RowVersion,
        };
    }

    public sealed class ClientRepository : ContactRepository<Client>
    {
        public ClientRepository(DbConnectionFactory db, IAuditContext audit)
            : base(db, audit, "Clients", "Ya existe un cliente con ese nombre.")
        {
        }

        /// <summary>Active tests where the client is the client or the concrete provider, newest first.</summary>
        public Task<RecordUsage> GetUsageAsync(int id, CancellationToken cancellationToken = default) => GetUsageAsync(
            "FROM CompressionTests t WHERE t.IsArchived = 0 AND (t.ClientId = @id OR t.ConcreteProviderId = @id)",
            TestNameExpression, "t.TestDate DESC, t.Id DESC", id, cancellationToken);
    }

    public sealed class ProviderRepository : ContactRepository<Provider>
    {
        public ProviderRepository(DbConnectionFactory db, IAuditContext audit)
            : base(db, audit, "Providers", "Ya existe un proveedor con ese nombre.")
        {
        }

        /// <summary>Active materials of the provider.</summary>
        public Task<RecordUsage> GetUsageAsync(int id, CancellationToken cancellationToken = default) => GetUsageAsync(
            "FROM Materials m WHERE m.IsArchived = 0 AND m.ProviderId = @id", "m.Name", "m.Name", id, cancellationToken);
    }
}
