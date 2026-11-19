using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using ProbetaControl.Models;
using ProbetaControl.Services;

namespace ProbetaControl.Data
{
    /// <summary>
    /// Photos of probetas. Adding writes an audit row (without the image); deleting goes through the stored
    /// procedure DeleteSpecimenPhoto (script 014), the only delete the app's account can do.
    /// </summary>
    public sealed class SpecimenPhotoRepository : RepositoryBase
    {
        public const string Table = "SpecimenPhotos";

        /// <summary>ER_SIGNAL_EXCEPTION: DeleteSpecimenPhoto found no such photo.</summary>
        private const int SignalException = 1644;

        public SpecimenPhotoRepository(DbConnectionFactory db, IAuditContext audit)
            : base(db, audit)
        {
        }

        /// <summary>The probeta's photos, oldest first, images included (a probeta has at most a few).</summary>
        public async Task<IReadOnlyList<SpecimenPhoto>> ListAsync(int specimenId, CancellationToken cancellationToken = default)
        {
            const string sql =
                "SELECT p.Id, p.SpecimenId, p.Photo, p.ContentType, p.SizeBytes, p.WidthPx, p.HeightPx, p.CreatedAt, p.CreatedBy, " +
                "       COALESCE(NULLIF(u.FullName, ''), u.UserName) AS CreatedByName " +
                "FROM SpecimenPhotos p JOIN Users u ON u.Id = p.CreatedBy " +
                "WHERE p.SpecimenId = @specimenId ORDER BY p.Id";

            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return (await connection.QueryAsync<SpecimenPhoto>(Cmd(sql, new { specimenId }, null, cancellationToken)).ConfigureAwait(false)).AsList();
            }
        }

        /// <summary>
        /// Adds <paramref name="photo"/> (replacing <paramref name="replaceId"/> when given) in one transaction, with the
        /// probeta locked so two PCs can't pass <paramref name="maxPerSpecimen"/> together. Fills Id and CreatedAt.
        /// </summary>
        public Task AddAsync(SpecimenPhoto photo, int? replaceId, int maxPerSpecimen, CancellationToken cancellationToken = default)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                var exists = await connection.ExecuteScalarAsync<int?>(Cmd(
                    "SELECT Id FROM Specimens WHERE Id = @SpecimenId FOR UPDATE", new { photo.SpecimenId }, transaction, cancellationToken)).ConfigureAwait(false);
                if (exists == null)
                {
                    throw new NotFoundException("la probeta");
                }

                if (replaceId.HasValue)
                {
                    var owner = await connection.ExecuteScalarAsync<int?>(Cmd(
                        "SELECT SpecimenId FROM SpecimenPhotos WHERE Id = @replaceId", new { replaceId }, transaction, cancellationToken)).ConfigureAwait(false);
                    if (owner != photo.SpecimenId)
                    {
                        throw new NotFoundException("la foto (otra persona pudo quitarla)");
                    }

                    await CallDeleteAsync(connection, transaction, replaceId.Value, cancellationToken).ConfigureAwait(false);
                }

                var count = await connection.ExecuteScalarAsync<int>(Cmd(
                    "SELECT COUNT(*) FROM SpecimenPhotos WHERE SpecimenId = @SpecimenId", new { photo.SpecimenId }, transaction, cancellationToken)).ConfigureAwait(false);
                if (count >= maxPerSpecimen)
                {
                    throw new ValidationException("Photo", $"La probeta ya tiene {maxPerSpecimen} fotos. Quite o cambie una.");
                }

                photo.Id = await connection.ExecuteScalarAsync<int>(Cmd(
                    "INSERT INTO SpecimenPhotos (SpecimenId, Photo, ContentType, WidthPx, HeightPx, CreatedBy) " +
                    "VALUES (@SpecimenId, @Photo, @ContentType, @WidthPx, @HeightPx, @CreatedBy); SELECT LAST_INSERT_ID();",
                    photo, transaction, cancellationToken)).ConfigureAwait(false);
                photo.CreatedAt = await connection.ExecuteScalarAsync<System.DateTime>(Cmd(
                    "SELECT CreatedAt FROM SpecimenPhotos WHERE Id = @Id", new { photo.Id }, transaction, cancellationToken)).ConfigureAwait(false);
                photo.SizeBytes = photo.Photo.Length;

                await AuditWriter.WriteAsync(connection, transaction, Audit, Table, photo.Id, AuditActions.Insert,
                    AuditJson.Object(photo.GetAuditValues()), cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>Really deletes the photo; the procedure writes the audit row in the same transaction.</summary>
        public Task DeleteAsync(int photoId, CancellationToken cancellationToken = default) =>
            InTransactionAsync((connection, transaction) => CallDeleteAsync(connection, transaction, photoId, cancellationToken), cancellationToken);

        private async Task CallDeleteAsync(MySqlConnection connection, MySqlTransaction transaction, int photoId, CancellationToken cancellationToken)
        {
            try
            {
                await connection.ExecuteAsync(Cmd("CALL DeleteSpecimenPhoto(@photoId, @UserId, @MachineName)",
                    new { photoId, Audit.UserId, MachineName = Truncate(Audit.MachineName, 64) }, transaction, cancellationToken)).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == SignalException)
            {
                throw new NotFoundException("la foto (otra persona pudo quitarla)");
            }
        }

        private static string Truncate(string text, int length) =>
            text == null || text.Length <= length ? text : text.Substring(0, length);
    }
}
