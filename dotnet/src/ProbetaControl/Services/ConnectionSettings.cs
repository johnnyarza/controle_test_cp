using MySqlConnector;

namespace ProbetaControl.Services
{
    /// <summary>Where the app's database is and which account it uses (stored in ProbetaControl.config).</summary>
    public sealed class ConnectionSettings
    {
        public const uint DefaultPort = 3306;
        public const string DefaultDatabase = "probeta_control";

        public string Host { get; set; }

        public uint Port { get; set; } = DefaultPort;

        public string Database { get; set; } = DefaultDatabase;

        public string UserName { get; set; }

        public string Password { get; set; }

        public string BuildConnectionString()
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = Host,
                Port = Port,
                Database = Database,
                UserID = UserName,
                Password = Password ?? string.Empty,
                ConnectionTimeout = 10,
                DefaultCommandTimeout = 60,
                Pooling = true,
                ApplicationName = "ProbetaControl",
            };

            return builder.ConnectionString;
        }

        /// <summary>Same server and account, but logged in as a different user (e.g. specimen_admin for migrations).</summary>
        public ConnectionSettings WithCredentials(string userName, string password) => new ConnectionSettings
        {
            Host = Host,
            Port = Port,
            Database = Database,
            UserName = userName,
            Password = password,
        };
    }
}
