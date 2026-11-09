using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ProbetaControl.Models;

namespace ProbetaControl.Data
{
    public sealed class UserRepository : RepositoryBase
    {
        private const string Table = "Users";
        private const string DuplicateMessage = "Ya existe un usuario con ese nombre.";
        private const string Columns = "Id, UserName, FullName, PasswordHash, Role, MustChangePassword, IsArchived, RowVersion";

        public UserRepository(DbConnectionFactory db, IAuditContext audit)
            : base(db, audit)
        {
        }

        public async Task<User> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await connection.QuerySingleOrDefaultAsync<User>(
                    Cmd($"SELECT {Columns} FROM Users WHERE UserName = @userName", new { userName = userName?.Trim() }, null, cancellationToken)).ConfigureAwait(false);
            }
        }

        public async Task<User> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await connection.QuerySingleOrDefaultAsync<User>(
                    Cmd($"SELECT {Columns} FROM Users WHERE Id = @id", new { id }, null, cancellationToken)).ConfigureAwait(false);
            }
        }

        /// <summary>All users (a handful), ordered by user name.</summary>
        public async Task<IReadOnlyList<User>> ListAsync(bool includeArchived, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                var rows = await connection.QueryAsync<User>(Cmd(
                    $"SELECT {Columns} FROM Users WHERE (@includeArchived OR IsArchived = 0) ORDER BY UserName",
                    new { includeArchived }, null, cancellationToken)).ConfigureAwait(false);
                return rows.ToList();
            }
        }

        public async Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await connection.ExecuteScalarAsync<int>(Cmd(
                    "SELECT COUNT(*) FROM Users WHERE Role = 'admin' AND IsArchived = 0", null, null, cancellationToken)).ConfigureAwait(false);
            }
        }

        public Task InsertAsync(User user, CancellationToken cancellationToken = default)
        {
            return WithUniqueCheckAsync(() => InTransactionAsync(async (connection, transaction) =>
            {
                var id = await connection.ExecuteScalarAsync<int>(Cmd(
                    "INSERT INTO Users (UserName, FullName, PasswordHash, Role, MustChangePassword) " +
                    "VALUES (@UserName, @FullName, @PasswordHash, @Role, @MustChangePassword); SELECT LAST_INSERT_ID();",
                    new { UserName = Clean(user.UserName), FullName = Clean(user.FullName), user.PasswordHash, user.Role, user.MustChangePassword },
                    transaction, cancellationToken)).ConfigureAwait(false);

                user.Id = id;
                user.RowVersion = 1;
                await AuditWriter.InsertedAsync(connection, transaction, Audit, Table, id, user, cancellationToken).ConfigureAwait(false);
                return id;
            }, cancellationToken), "UserName", DuplicateMessage);
        }

        /// <summary>Name, full name and role. The password changes only through <see cref="SetPasswordAsync"/>.</summary>
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            return WithUniqueCheckAsync(() => InTransactionAsync(async (connection, transaction) =>
            {
                var before = await connection.QuerySingleOrDefaultAsync<User>(Cmd(
                    $"SELECT {Columns} FROM Users WHERE Id = @Id FOR UPDATE", new { user.Id }, transaction, cancellationToken)).ConfigureAwait(false);

                var rows = await connection.ExecuteAsync(Cmd(
                    "UPDATE Users SET UserName = @UserName, FullName = @FullName, Role = @Role, RowVersion = RowVersion + 1 " +
                    "WHERE Id = @Id AND RowVersion = @RowVersion",
                    new { user.Id, UserName = Clean(user.UserName), FullName = Clean(user.FullName), user.Role, user.RowVersion },
                    transaction, cancellationToken)).ConfigureAwait(false);
                await EnsureUpdatedAsync(connection, transaction, Table, user.Id, rows, cancellationToken).ConfigureAwait(false);

                await AuditWriter.UpdatedAsync(connection, transaction, Audit, Table, user.Id, before, user, cancellationToken).ConfigureAwait(false);
                user.RowVersion++;
                return true;
            }, cancellationToken), "UserName", DuplicateMessage);
        }

        /// <summary>Stores a new hash. The audit row says the password changed, never the hash.</summary>
        public Task SetPasswordAsync(int userId, string passwordHash, bool mustChangePassword, CancellationToken cancellationToken = default)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                var rows = await connection.ExecuteAsync(Cmd(
                    "UPDATE Users SET PasswordHash = @passwordHash, MustChangePassword = @mustChangePassword, RowVersion = RowVersion + 1 WHERE Id = @userId",
                    new { userId, passwordHash, mustChangePassword }, transaction, cancellationToken)).ConfigureAwait(false);
                await EnsureUpdatedAsync(connection, transaction, Table, userId, rows, cancellationToken).ConfigureAwait(false);

                var changes = AuditJson.Object(new Dictionary<string, object>
                {
                    ["Password"] = "changed",
                    ["MustChangePassword"] = mustChangePassword,
                });
                await AuditWriter.WriteAsync(connection, transaction, Audit, Table, userId, AuditActions.Update, changes, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public async Task SetArchivedAsync(User user, bool archived, CancellationToken cancellationToken = default)
        {
            user.RowVersion = await SetArchivedAsync(Table, user.Id, user.RowVersion, archived, cancellationToken).ConfigureAwait(false);
            user.IsArchived = archived;
        }
    }
}
