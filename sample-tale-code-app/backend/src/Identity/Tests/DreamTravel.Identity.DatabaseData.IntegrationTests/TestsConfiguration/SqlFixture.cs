using System.Data.SqlClient;
using System.Threading.Tasks;
using DreamTravel.Infrastructure.Database;
using Xunit;

namespace DreamTravel.Identity.DatabaseData.IntegrationTests.TestsConfiguration
{
    public class SqlFixture : IAsyncLifetime
    {
        public IDbConnectionFactory DbConnectionFactory;
        public SqlConnection SqlConnection { get; private set; }
        private string _connectionString;

        public async Task InitializeAsync()
        {
            var config = new SqlDatabaseConfiguration
            {
                ConnectionString =
                    "Data Source=localhost,1401;Database=DreamTravelDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
            };
            _connectionString = config.ConnectionString;

            DbConnectionFactory = new DbConnectionFactory(config);

            SqlConnection?.Dispose();
            SqlConnection = new SqlConnection(_connectionString);
            SqlConnection.Open();

            await new Respawn.Checkpoint().Reset(_connectionString);
        }

        public async Task DisposeAsync()
        {
            SqlConnection?.Dispose();
            await new Respawn.Checkpoint().Reset(_connectionString);
        }
    }
}
