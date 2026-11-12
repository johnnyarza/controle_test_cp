using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace ProbetaControl.Data
{
    /// <summary>MySQL server version and schema version, for "Acerca de" and support.</summary>
    public sealed class ServerInfoRepository : RepositoryBase
    {
        public ServerInfoRepository(DbConnectionFactory db, IAuditContext audit)
            : base(db, audit)
        {
        }

        public async Task<ServerInfo> GetAsync(CancellationToken cancellationToken = default)
        {
            using (var connection = await Db.OpenAsync(cancellationToken).ConfigureAwait(false))
            using (var results = await connection.QueryMultipleAsync(Cmd(
                "SELECT VERSION(); SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion", null, null, cancellationToken)).ConfigureAwait(false))
            {
                return new ServerInfo
                {
                    ServerVersion = await results.ReadSingleAsync<string>().ConfigureAwait(false),
                    SchemaVersion = await results.ReadSingleAsync<int>().ConfigureAwait(false),
                };
            }
        }
    }

    public sealed class ServerInfo
    {
        public string ServerVersion { get; set; }

        public int SchemaVersion { get; set; }
    }
}
