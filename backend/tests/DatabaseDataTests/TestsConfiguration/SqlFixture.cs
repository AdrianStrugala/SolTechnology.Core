using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using DreamTravel.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DreamTravel.DatabaseDataTests.TestsConfiguration
{
    public class SqlFixture : IAsyncLifetime
    {
        public IDbConnectionFactory DbConnectionFactory;
        public SqlConnection SqlConnection { get; private set; }
        private string _connectionString;

        public async Task InitializeAsync()
        {
            _connectionString =
                "Data Source=localhost,1433;Database=DreamTravelDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True";
            DbConnectionFactory = new DbConnectionFactory(_connectionString);

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
