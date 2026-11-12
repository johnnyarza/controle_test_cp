using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ProbetaControl.Models;

namespace ProbetaControl.Data
{
    public sealed class CompressionTestRepository : RepositoryBase
    {
        private const string Table = "CompressionTests";

        private const string Select =
            "SELECT t.Id, t.ClientId, t.ConcreteProviderId, t.ConcreteDesignId, t.ProjectName, t.Address, t.TestDate, " +
            "t.IsArchived, t.RowVersion, c.Name AS ClientName, cp.Name AS ConcreteProviderName, d.Description AS ConcreteDesignDescription " +
            "FROM CompressionTests t " +
            "JOIN Clients c ON c.Id = t.ClientId " +
            "JOIN Clients cp ON cp.Id = t.ConcreteProviderId " +
            "JOIN ConcreteDesigns d ON d.Id = t.ConcreteDesignId ";

        /// <summary>Untested, active specimens of test t due today (@today) or earlier.</summary>
        private const string DueSpecimen =
            "SELECT 1 FROM Specimens s WHERE s.CompressionTestId = t.Id AND s.IsArchived = 0 " +
            "AND s.RuptureLoadTon IS NULL AND s.RuptureDate <= @today";

        public CompressionTestRepository(DbConnectionFactory db, IAuditContext audit)
            : base(db, audit)
        {
        }

        /// <summary>Active tests with at least one specimen to break today or earlier.</summary>
        public async Task<int> CountWithDueAsync(DateTime today, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await connection.ExecuteScalarAsync<int>(Cmd(
                    "SELECT COUNT(*) FROM CompressionTests t WHERE t.IsArchived = 0 AND EXISTS (" + DueSpecimen + ")",
                    new { today = today.Date }, null, cancellationToken)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// One page, newest first. Text matches the test number, client, project or address.
        /// Counts are per row of the page only, so a page stays fast however big the table is.
        /// </summary>
        public async Task<PagedResult<CompressionTestListItem>> SearchAsync(CompressionTestQuery query, DateTime today,
            CancellationToken cancellationToken = default)
        {
            const string from = "FROM CompressionTests t JOIN Clients c ON c.Id = t.ClientId ";
            const string where =
                "WHERE (@includeArchived OR t.IsArchived = 0) " +
                "AND (@clientId IS NULL OR t.ClientId = @clientId) " +
                "AND (@dateFrom IS NULL OR t.TestDate >= @dateFrom) " +
                "AND (@dateTo IS NULL OR t.TestDate <= @dateTo) " +
                "AND (@pattern IS NULL OR t.Id = @number OR c.Name LIKE @pattern OR t.ProjectName LIKE @pattern OR t.Address LIKE @pattern) " +
                "AND (NOT @onlyDue OR EXISTS (" + DueSpecimen + ")) ";

            var sql =
                "SELECT COUNT(*) " + from + where + "; " +
                "SELECT t.Id, t.ClientId, c.Name AS ClientName, t.ProjectName, t.Address, t.TestDate, t.IsArchived, " +
                "  (SELECT COUNT(*) FROM Specimens s WHERE s.CompressionTestId = t.Id AND s.IsArchived = 0) AS SpecimenCount, " +
                "  (" + DueSpecimen.Replace("SELECT 1", "SELECT COUNT(*)") + ") AS DueCount " +
                from + where + "ORDER BY t.TestDate DESC, t.Id DESC LIMIT @limit OFFSET @offset";

            int.TryParse(query.Text?.Trim(), out var number);

            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            using (var results = await connection.QueryMultipleAsync(Cmd(sql, new
            {
                includeArchived = query.IncludeArchived,
                clientId = query.ClientId,
                dateFrom = query.DateFrom?.Date,
                dateTo = query.DateTo?.Date,
                pattern = Contains(query.Text),
                number,
                onlyDue = query.OnlyDue,
                today = today.Date,
                limit = query.Page.Limit,
                offset = query.Page.Offset,
            }, null, cancellationToken)).ConfigureAwait(false))
            {
                var total = await results.ReadSingleAsync<int>().ConfigureAwait(false);
                var items = (await results.ReadAsync<CompressionTestListItem>().ConfigureAwait(false)).ToList();
                return new PagedResult<CompressionTestListItem>(items, total, query.Page);
            }
        }

        public async Task<CompressionTest> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await connection.QuerySingleOrDefaultAsync<CompressionTest>(
                    Cmd(Select + "WHERE t.Id = @id", new { id }, null, cancellationToken)).ConfigureAwait(false);
            }
        }

        public Task InsertAsync(CompressionTest test, CancellationToken cancellationToken = default)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                var id = await connection.ExecuteScalarAsync<int>(Cmd(
                    "INSERT INTO CompressionTests (ClientId, ConcreteProviderId, ConcreteDesignId, ProjectName, Address, TestDate) " +
                    "VALUES (@ClientId, @ConcreteProviderId, @ConcreteDesignId, @ProjectName, @Address, @TestDate); SELECT LAST_INSERT_ID();",
                    Parameters(test), transaction, cancellationToken)).ConfigureAwait(false);

                test.Id = id;
                test.RowVersion = 1;
                await AuditWriter.InsertedAsync(connection, transaction, Audit, Table, id, test, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public Task UpdateAsync(CompressionTest test, CancellationToken cancellationToken = default)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                var before = await connection.QuerySingleOrDefaultAsync<CompressionTest>(Cmd(
                    "SELECT Id, ClientId, ConcreteProviderId, ConcreteDesignId, ProjectName, Address, TestDate " +
                    "FROM CompressionTests WHERE Id = @Id FOR UPDATE", new { test.Id }, transaction, cancellationToken)).ConfigureAwait(false);

                var rows = await connection.ExecuteAsync(Cmd(
                    "UPDATE CompressionTests SET ClientId = @ClientId, ConcreteProviderId = @ConcreteProviderId, " +
                    "ConcreteDesignId = @ConcreteDesignId, ProjectName = @ProjectName, Address = @Address, TestDate = @TestDate, " +
                    "RowVersion = RowVersion + 1 WHERE Id = @Id AND RowVersion = @RowVersion",
                    Parameters(test), transaction, cancellationToken)).ConfigureAwait(false);
                await EnsureUpdatedAsync(connection, transaction, Table, test.Id, rows, cancellationToken).ConfigureAwait(false);

                await AuditWriter.UpdatedAsync(connection, transaction, Audit, Table, test.Id, before, test, cancellationToken).ConfigureAwait(false);
                test.RowVersion++;
            }, cancellationToken);
        }

        public async Task SetArchivedAsync(CompressionTest test, bool archived, CancellationToken cancellationToken = default)
        {
            test.RowVersion = await SetArchivedAsync(Table, test.Id, test.RowVersion, archived, cancellationToken).ConfigureAwait(false);
            test.IsArchived = archived;
        }

        private static object Parameters(CompressionTest test) => new
        {
            test.Id,
            test.ClientId,
            test.ConcreteProviderId,
            test.ConcreteDesignId,
            ProjectName = Clean(test.ProjectName),
            Address = Clean(test.Address),
            TestDate = test.TestDate.Date,
            test.RowVersion,
        };
    }
}
