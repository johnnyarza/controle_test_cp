using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ProbetaControl.Models;

namespace ProbetaControl.Data
{
    public sealed class SpecimenRepository : RepositoryBase
    {
        private const string Table = "Specimens";

        private const string Columns =
            "s.Id, s.CompressionTestId, s.Code, s.Slump, s.MoldDate, s.RuptureDate, s.DiameterCm, s.HeightCm, s.WeightKg, " +
            "s.RuptureLoadTon, s.IsLocked, s.IsArchived, s.RowVersion";

        public SpecimenRepository(DbConnectionFactory db, IAuditContext audit)
            : base(db, audit)
        {
        }

        /// <summary>Specimens of one test (a test holds tens of rows, so no paging), ordered by ID.</summary>
        public async Task<IReadOnlyList<Specimen>> ListByTestAsync(int compressionTestId, SpecimenFilter filter,
            CancellationToken cancellationToken = default)
        {
            filter = filter ?? SpecimenFilter.None;
            var parameters = new DynamicParameters(new
            {
                compressionTestId,
                includeArchived = filter.IncludeArchived,
                moldFrom = filter.MoldDateFrom?.Date,
                moldTo = filter.MoldDateTo?.Date,
            });

            var sql = new StringBuilder($"SELECT {Columns} FROM Specimens s WHERE s.CompressionTestId = @compressionTestId ")
                .Append("AND (@includeArchived OR s.IsArchived = 0) ")
                .Append("AND (@moldFrom IS NULL OR s.MoldDate >= @moldFrom) ")
                .Append("AND (@moldTo IS NULL OR s.MoldDate <= @moldTo) ");

            if (filter.IdRanges.Count > 0)
            {
                // Each range becomes its own parameters; no user text ever reaches the SQL.
                var conditions = filter.IdRanges.Select((range, i) =>
                {
                    parameters.Add("from" + i, range.From);
                    parameters.Add("to" + i, range.To);
                    return $"s.Id BETWEEN @from{i} AND @to{i}";
                });
                sql.Append("AND (").Append(string.Join(" OR ", conditions)).Append(") ");
            }

            sql.Append("ORDER BY s.Id");

            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                var rows = await connection.QueryAsync<Specimen>(Cmd(sql.ToString(), parameters, null, cancellationToken)).ConfigureAwait(false);
                return rows.ToList();
            }
        }

        public async Task<Specimen> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await connection.QuerySingleOrDefaultAsync<Specimen>(
                    Cmd($"SELECT {Columns} FROM Specimens s WHERE s.Id = @id", new { id }, null, cancellationToken)).ConfigureAwait(false);
            }
        }

        /// <summary>Untested specimens of a test whose rupture date is today or earlier ("probetas para romper").</summary>
        public async Task<int> CountDueAsync(int compressionTestId, DateTime today, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await connection.ExecuteScalarAsync<int>(Cmd(
                    "SELECT COUNT(*) FROM Specimens WHERE CompressionTestId = @compressionTestId AND IsArchived = 0 " +
                    "AND RuptureLoadTon IS NULL AND RuptureDate <= @today",
                    new { compressionTestId, today = today.Date }, null, cancellationToken)).ConfigureAwait(false);
            }
        }

        /// <summary>Untested specimens of all tests due up to <paramref name="through"/> (overdue included), oldest first.</summary>
        public async Task<IReadOnlyList<DueSpecimen>> ListDueAsync(DateTime through, CancellationToken cancellationToken = default)
        {
            var sql =
                $"SELECT {Columns}, c.Name AS ClientName, t.ProjectName, d.Description AS ConcreteDesignDescription FROM Specimens s " +
                "JOIN CompressionTests t ON t.Id = s.CompressionTestId " +
                "JOIN Clients c ON c.Id = t.ClientId " +
                "JOIN ConcreteDesigns d ON d.Id = t.ConcreteDesignId " +
                "WHERE s.IsArchived = 0 AND t.IsArchived = 0 AND s.RuptureLoadTon IS NULL AND s.RuptureDate <= @through " +
                "ORDER BY s.RuptureDate, s.Id";

            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                var rows = await connection.QueryAsync<DueSpecimen>(Cmd(sql, new { through = through.Date }, null, cancellationToken)).ConfigureAwait(false);
                return rows.ToList();
            }
        }

        public Task InsertAsync(Specimen specimen, CancellationToken cancellationToken = default)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                var id = await connection.ExecuteScalarAsync<int>(Cmd(
                    "INSERT INTO Specimens (CompressionTestId, Code, Slump, MoldDate, RuptureDate, DiameterCm, HeightCm, WeightKg, RuptureLoadTon, IsLocked) " +
                    "VALUES (@CompressionTestId, @Code, @Slump, @MoldDate, @RuptureDate, @DiameterCm, @HeightCm, @WeightKg, @RuptureLoadTon, @IsLocked); " +
                    "SELECT LAST_INSERT_ID();",
                    Parameters(specimen), transaction, cancellationToken)).ConfigureAwait(false);

                specimen.Id = id;
                specimen.RowVersion = 1;
                await AuditWriter.InsertedAsync(connection, transaction, Audit, Table, id, specimen, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public Task UpdateAsync(Specimen specimen, CancellationToken cancellationToken = default)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                var before = await connection.QuerySingleOrDefaultAsync<Specimen>(Cmd(
                    $"SELECT {Columns} FROM Specimens s WHERE s.Id = @Id FOR UPDATE", new { specimen.Id }, transaction, cancellationToken)).ConfigureAwait(false);

                var rows = await connection.ExecuteAsync(Cmd(
                    "UPDATE Specimens SET CompressionTestId = @CompressionTestId, Code = @Code, Slump = @Slump, MoldDate = @MoldDate, " +
                    "RuptureDate = @RuptureDate, DiameterCm = @DiameterCm, HeightCm = @HeightCm, WeightKg = @WeightKg, " +
                    "RuptureLoadTon = @RuptureLoadTon, IsLocked = @IsLocked, RowVersion = RowVersion + 1 " +
                    "WHERE Id = @Id AND RowVersion = @RowVersion",
                    Parameters(specimen), transaction, cancellationToken)).ConfigureAwait(false);
                await EnsureUpdatedAsync(connection, transaction, Table, specimen.Id, rows, cancellationToken).ConfigureAwait(false);

                await AuditWriter.UpdatedAsync(connection, transaction, Audit, Table, specimen.Id, before, specimen, cancellationToken).ConfigureAwait(false);
                specimen.RowVersion++;
            }, cancellationToken);
        }

        public async Task SetArchivedAsync(Specimen specimen, bool archived, CancellationToken cancellationToken = default)
        {
            specimen.RowVersion = await SetArchivedAsync(Table, specimen.Id, specimen.RowVersion, archived, cancellationToken).ConfigureAwait(false);
            specimen.IsArchived = archived;
        }

        private static object Parameters(Specimen specimen) => new
        {
            specimen.Id,
            specimen.CompressionTestId,
            Code = Clean(specimen.Code),
            specimen.Slump,
            MoldDate = specimen.MoldDate.Date,
            RuptureDate = specimen.RuptureDate.Date,
            specimen.DiameterCm,
            specimen.HeightCm,
            specimen.WeightKg,
            specimen.RuptureLoadTon,
            specimen.IsLocked,
            specimen.RowVersion,
        };
    }
}
