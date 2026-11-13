using System.Collections.Generic;
using System.Linq;
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
        public const string ReportLabName = "ReportLabName";
        public const string ReportFooter = "ReportFooter";
        public const string ReportNote = "ReportNote";
        public const string ReportPaperSize = "ReportPaperSize";

        /// <summary>Text settings by key (missing keys are absent; a null value means empty).</summary>
        public async Task<IReadOnlyDictionary<string, string>> GetTextsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                var rows = await connection.QueryAsync<(string SettingKey, string TextValue)>(Cmd(
                    "SELECT SettingKey, TextValue FROM AppSettings WHERE SettingKey IN @keys",
                    new { keys = keys.ToArray() }, null, cancellationToken)).ConfigureAwait(false);
                return rows.ToDictionary(r => r.SettingKey, r => r.TextValue);
            }
        }

        /// <summary>Changes several text settings in one transaction; one audit row per setting that changed.</summary>
        public Task SetTextsAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default)
        {
            return InTransactionAsync(async (connection, transaction) =>
            {
                foreach (var pair in values)
                {
                    var row = await connection.QuerySingleOrDefaultAsync<(int Id, string TextValue)?>(Cmd(
                        "SELECT Id, TextValue FROM AppSettings WHERE SettingKey = @key FOR UPDATE",
                        new { key = pair.Key }, transaction, cancellationToken)).ConfigureAwait(false);
                    if (row == null)
                    {
                        throw new Services.NotFoundException($"el ajuste {pair.Key}");
                    }

                    if (row.Value.TextValue == pair.Value)
                    {
                        continue;
                    }

                    await connection.ExecuteAsync(Cmd(
                        "UPDATE AppSettings SET TextValue = @value, RowVersion = RowVersion + 1 WHERE Id = @id",
                        new { id = row.Value.Id, value = pair.Value }, transaction, cancellationToken)).ConfigureAwait(false);

                    var changes = AuditJson.Object(new Dictionary<string, object>
                    {
                        ["SettingKey"] = pair.Key,
                        ["TextValue"] = pair.Value,
                    });
                    await AuditWriter.WriteAsync(connection, transaction, Audit, "AppSettings", row.Value.Id, AuditActions.Update, changes, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

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
