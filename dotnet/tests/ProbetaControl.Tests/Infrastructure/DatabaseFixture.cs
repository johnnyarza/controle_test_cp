using System.Threading.Tasks;
using MySqlConnector;
using NUnit.Framework;

namespace ProbetaControl.Tests.Infrastructure
{
    /// <summary>
    /// Base for tests that need a real database. Runs once per server in TEST_SERVERS with its own
    /// throwaway database; ignored (with a reason) when test.env is missing or incomplete.
    /// </summary>
    [TestFixtureSource(typeof(TestServer), nameof(TestServer.FixtureSource))]
    [NonParallelizable]
    public abstract class DatabaseFixture
    {
        private readonly TestServer _server;

        protected DatabaseFixture(TestServer server)
        {
            _server = server;
        }

        protected TestServer Server => _server;

        protected ThrowawayDatabase Database { get; private set; }

        [OneTimeSetUp]
        public async Task CreateDatabaseAsync()
        {
            if (_server == null)
            {
                Assert.Ignore("Database tests skipped: copy dotnet/tests/test.env.example to test.env and fill it in.");
            }

            Database = await ThrowawayDatabase.CreateAsync(_server, TestSettings.Current);
        }

        [OneTimeTearDown]
        public async Task DropDatabaseAsync()
        {
            if (Database != null)
            {
                await Database.DropAsync();
            }
        }

        protected async Task<MySqlConnection> OpenAsAppAsync()
        {
            var connection = new MySqlConnection(Database.AppConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        protected async Task<MySqlConnection> OpenAsAdminAsync()
        {
            var connection = new MySqlConnection(Database.AdminConnectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}
