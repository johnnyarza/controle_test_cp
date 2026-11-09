using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using ProbetaControl.Models;

namespace ProbetaControl.Data
{
    public static class AuditActions
    {
        public const string Insert = "insert";
        public const string Update = "update";
        public const string Archive = "archive";
        public const string Restore = "restore";
    }

    /// <summary>
    /// Writes AuditLog rows. Always called with the transaction of the change it describes,
    /// so the change and its audit row are committed (or rolled back) together.
    /// </summary>
    internal static class AuditWriter
    {
        public static Task InsertedAsync(MySqlConnection connection, MySqlTransaction transaction, IAuditContext context,
            string table, int recordId, IAuditable record, CancellationToken cancellationToken)
        {
            var json = AuditJson.Object(record.GetAuditValues());
            return WriteAsync(connection, transaction, context, table, recordId, AuditActions.Insert, json, cancellationToken);
        }

        /// <summary>Writes the changed fields as {"Field": {"old": .., "new": ..}}. Writes nothing if nothing changed.</summary>
        public static Task UpdatedAsync(MySqlConnection connection, MySqlTransaction transaction, IAuditContext context,
            string table, int recordId, IAuditable before, IAuditable after, CancellationToken cancellationToken)
        {
            var changes = Diff(before.GetAuditValues(), after.GetAuditValues());
            if (changes.Count == 0)
            {
                return Task.CompletedTask;
            }

            return WriteAsync(connection, transaction, context, table, recordId, AuditActions.Update, AuditJson.Object(changes), cancellationToken);
        }

        public static Task WriteAsync(MySqlConnection connection, MySqlTransaction transaction, IAuditContext context,
            string table, int recordId, string action, string changesJson, CancellationToken cancellationToken)
        {
            const string sql =
                "INSERT INTO AuditLog (UserId, MachineName, TableName, RecordId, Action, Changes) " +
                "VALUES (@UserId, @MachineName, @TableName, @RecordId, @Action, @Changes)";

            return connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                context.UserId,
                MachineName = Truncate(context.MachineName, 64),
                TableName = table,
                RecordId = recordId,
                Action = action,
                Changes = changesJson,
            }, transaction, cancellationToken: cancellationToken));
        }

        internal static Dictionary<string, object> Diff(IReadOnlyDictionary<string, object> before, IReadOnlyDictionary<string, object> after)
        {
            var changes = new Dictionary<string, object>();
            foreach (var key in after.Keys.Union(before.Keys))
            {
                before.TryGetValue(key, out var oldValue);
                after.TryGetValue(key, out var newValue);
                if (!Equals(Normalize(oldValue), Normalize(newValue)))
                {
                    changes[key] = new Dictionary<string, object> { ["old"] = oldValue, ["new"] = newValue };
                }
            }

            return changes;
        }

        // Treats "" and null as equal. (Boxed decimals already compare by value: 1.50m equals 1.5m.)
        private static object Normalize(object value) => value is string s && s.Length == 0 ? null : value;

        private static string Truncate(string value, int max) =>
            value == null || value.Length <= max ? value : value.Substring(0, max);
    }

    /// <summary>Minimal JSON writer for audit values (strings, numbers, booleans, dates, nested objects).</summary>
    internal static class AuditJson
    {
        public static string Object(IEnumerable<KeyValuePair<string, object>> values)
        {
            var builder = new StringBuilder();
            WriteObject(builder, values);
            return builder.ToString();
        }

        private static void WriteObject(StringBuilder builder, IEnumerable<KeyValuePair<string, object>> values)
        {
            builder.Append('{');
            var first = true;
            foreach (var pair in values)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                WriteString(builder, pair.Key);
                builder.Append(':');
                WriteValue(builder, pair.Value);
            }

            builder.Append('}');
        }

        private static void WriteValue(StringBuilder builder, object value)
        {
            switch (value)
            {
                case null:
                    builder.Append("null");
                    break;
                case string s:
                    WriteString(builder, s);
                    break;
                case bool b:
                    builder.Append(b ? "true" : "false");
                    break;
                case DateTime date:
                    WriteString(builder, date.TimeOfDay == TimeSpan.Zero
                        ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    break;
                case IEnumerable<KeyValuePair<string, object>> nested:
                    WriteObject(builder, nested);
                    break;
                case IFormattable number when value is int || value is long || value is decimal || value is double || value is float || value is short || value is byte:
                    builder.Append(number.ToString(null, CultureInfo.InvariantCulture));
                    break;
                default:
                    WriteString(builder, Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
            }
        }

        private static void WriteString(StringBuilder builder, string text)
        {
            builder.Append('"');
            foreach (var c in text)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            builder.Append('"');
        }
    }
}
