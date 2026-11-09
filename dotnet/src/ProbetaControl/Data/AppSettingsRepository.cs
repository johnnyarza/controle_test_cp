using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace ProbetaControl.Data
{
    /// <summary>Settings shared by every PC. The rows exist from the schema script, so only UPDATE is needed.</summary>
    public sealed class AppSettingsRepository : RepositoryBase
    {
        public const string ReportLogo = "ReportLogo";
        public const string ReportStamp = "ReportStamp";

        public AppSettingsRepository(DbConnectionFactory db, IAuditContext audit)
            : base(db, audit)
        {
        }

        /// <summary>Image bytes and content type, or null when no image was uploaded yet.</summary>
        public async Task<StoredImage> GetImageAsync(string key, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                var row = await connection.QuerySingleOrDefaultAsync<StoredImage>(Cmd(
                    "SELECT BinaryValue AS Bytes, ContentType FROM AppSettings WHERE SettingKey = @key",
                    new { key }, null, cancellationToken)).ConfigureAwait(false);
                return row?.Bytes == null ? null : row;
            }
        }

        /// <summary>Replaces (or with null, removes) an image. The audit row records size and type, not the bytes.</summary>
        public Task SetImageAsync(string key, byte[] bytes, string contentType, CancellationToken cancellationToken = default)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                var id = await connection.ExecuteScalarAsync<int?>(Cmd(
                    "SELECT Id FROM AppSettings WHERE SettingKey = @key FOR UPDATE", new { key }, transaction, cancellationToken)).ConfigureAwait(false);
                if (id == null)
                {
                    throw new Services.NotFoundException($"el ajuste {key}");
                }

                await connection.ExecuteAsync(Cmd(
                    "UPDATE AppSettings SET BinaryValue = @bytes, ContentType = @contentType, RowVersion = RowVersion + 1 WHERE Id = @id",
                    new { id, bytes, contentType = bytes == null ? null : contentType }, transaction, cancellationToken)).ConfigureAwait(false);

                var changes = AuditJson.Object(new Dictionary<string, object>
                {
                    ["SettingKey"] = key,
                    ["Bytes"] = bytes?.Length,
                    ["ContentType"] = bytes == null ? null : contentType,
                });
                await AuditWriter.WriteAsync(connection, transaction, Audit, "AppSettings", id.Value, AuditActions.Update, changes, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }

    public sealed class StoredImage
    {
        public byte[] Bytes { get; set; }

        public string ContentType { get; set; }
    }
}
