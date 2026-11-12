using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using ProbetaControl.Services;

namespace ProbetaControl.Data
{
    /// <summary>
    /// Shared plumbing for the Dapper repositories: transactions, optimistic concurrency
    /// (RowVersion), archive/restore and audit rows. The app's account has no DELETE right,
    /// so nothing here ever deletes.
    /// </summary>
    public abstract class RepositoryBase
    {
        /// <summary>ER_DUP_ENTRY.</summary>
        private const int DuplicateEntry = 1062;

        protected RepositoryBase(DbConnectionFactory db, IAuditContext audit)
        {
            Db = db ?? throw new ArgumentNullException(nameof(db));
            Audit = audit ?? throw new ArgumentNullException(nameof(audit));
        }

        protected DbConnectionFactory Db { get; }

        protected IAuditContext Audit { get; }

        protected static CommandDefinition Cmd(string sql, object parameters = null, MySqlTransaction transaction = null,
            CancellationToken cancellationToken = default) =>
            new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);

        /// <summary>Runs <paramref name="work"/> in a transaction; commits only if it completes.</summary>
        protected async Task<T> InTransactionAsync<T>(Func<MySqlConnection, MySqlTransaction, Task<T>> work, CancellationToken cancellationToken)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            using (var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
            {
                var result = await work(connection, transaction).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
        }

        protected Task InTransactionAsync(Func<MySqlConnection, MySqlTransaction, Task> work, CancellationToken cancellationToken) =>
            InTransactionAsync<bool>(async (connection, transaction) =>
            {
                await work(connection, transaction).ConfigureAwait(false);
                return true;
            }, cancellationToken);

        /// <summary>
        /// Counts the rows matched by <paramref name="fromWhere"/> ("FROM … WHERE …", parameter @id) and reads
        /// the first few of their names.
        /// </summary>
        protected async Task<Models.RecordUsage> GetUsageAsync(string fromWhere, string nameExpression, string orderBy, int id,
            CancellationToken cancellationToken)
        {
            var sql = $"SELECT COUNT(*) {fromWhere}; " +
                      $"SELECT {nameExpression} {fromWhere} ORDER BY {orderBy} LIMIT {Models.RecordUsage.MaxExamples}";

            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            using (var results = await connection.QueryMultipleAsync(Cmd(sql, new { id }, null, cancellationToken)).ConfigureAwait(false))
            {
                var count = await results.ReadSingleAsync<int>().ConfigureAwait(false);
                var names = (await results.ReadAsync<string>().ConfigureAwait(false)).AsList();
                return new Models.RecordUsage(count, names);
            }
        }

        /// <summary>A compression test (alias t) named as "Obra (dd/MM/yyyy)".</summary>
        protected const string TestNameExpression = "CONCAT(t.ProjectName, ' (', DATE_FORMAT(t.TestDate, '%d/%m/%Y'), ')')";

        /// <summary>
        /// Archives or restores a row if nobody changed it since it was read; writes the audit row.
        /// Returns the new RowVersion.
        /// </summary>
        protected Task<int> SetArchivedAsync(string table, int id, int rowVersion, bool archived, CancellationToken cancellationToken)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                var sql = $"UPDATE {table} SET IsArchived = @archived, RowVersion = RowVersion + 1 WHERE Id = @id AND RowVersion = @rowVersion";
                var rows = await connection.ExecuteAsync(Cmd(sql, new { id, rowVersion, archived }, transaction, cancellationToken)).ConfigureAwait(false);
                await EnsureUpdatedAsync(connection, transaction, table, id, rows, cancellationToken).ConfigureAwait(false);

                await AuditWriter.WriteAsync(connection, transaction, Audit, table, id,
                    archived ? AuditActions.Archive : AuditActions.Restore, null, cancellationToken).ConfigureAwait(false);
                return rowVersion + 1;
            }, cancellationToken);
        }

        /// <summary>Turns "0 rows updated" into NotFound or Concurrency, whichever applies.</summary>
        protected static async Task EnsureUpdatedAsync(MySqlConnection connection, MySqlTransaction transaction, string table, int id,
            int rowsAffected, CancellationToken cancellationToken)
        {
            if (rowsAffected > 0)
            {
                return;
            }

            var exists = await connection.ExecuteScalarAsync<int>(
                Cmd($"SELECT COUNT(*) FROM {table} WHERE Id = @id", new { id }, transaction, cancellationToken)).ConfigureAwait(false);
            if (exists == 0)
            {
                throw new NotFoundException("el registro");
            }

            throw new ConcurrencyException();
        }

        /// <summary>Runs a write and turns a unique-key violation into a Spanish field error.</summary>
        protected static async Task<T> WithUniqueCheckAsync<T>(Func<Task<T>> write, string field, string message)
        {
            try
            {
                return await write().ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == DuplicateEntry)
            {
                throw new ValidationException(field, message);
            }
        }

        /// <summary>LIKE pattern that matches <paramref name="text"/> anywhere, with % and _ taken literally.</summary>
        protected static string Contains(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var escaped = text.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            return "%" + escaped + "%";
        }

        protected static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
