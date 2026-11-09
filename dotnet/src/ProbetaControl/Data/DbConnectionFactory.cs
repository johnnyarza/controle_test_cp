using System;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace ProbetaControl.Data
{
    /// <summary>Opens connections with the app's account. Connections are pooled; open late, close early.</summary>
    public sealed class DbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<MySqlConnection> OpenAsync(CancellationToken cancellationToken = default)
        {
            var connection = new MySqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }
    }

    /// <summary>Who is making a change, for the audit log.</summary>
    public interface IAuditContext
    {
        int? UserId { get; }

        string MachineName { get; }
    }
}
