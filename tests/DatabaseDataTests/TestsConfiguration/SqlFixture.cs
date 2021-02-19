using System.Data.SqlClient;
using System.Threading.Tasks;
using DreamTravel.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DreamTravel.DatabaseDataTests.TestsConfiguration
{
    public class SqlFixture : IAsyncLifetime
    {
        public IDbConnectionFactory DbConnectionFactory;
        public DreamTravelsDbContext DbContext;
        public SqlConnection SqlConnection { get; private set; }
        private string _connectionString;

        public async Task InitializeAsync()
        {
            _connectionString =
                "Server=tcp:dreamtravel.database.windows.net,1433;Initial Catalog=dreamtravel-demo;Persist Security Info=False;User ID=adrian;Password=P4ssw0rd@;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;";
            DbConnectionFactory = new DbConnectionFactory(_connectionString);

            SqlConnection?.Dispose();
            SqlConnection = new SqlConnection(_connectionString);
            SqlConnection.Open();

            var dbContextOptions = new DbContextOptionsBuilder<DreamTravelsDbContext>();
            dbContextOptions
                .UseSqlServer(SqlConnection)
                .EnableSensitiveDataLogging(true)
                .EnableDetailedErrors(true);
            DbContext = new DreamTravelsDbContext(dbContextOptions.Options);

            await new Respawn.Checkpoint().Reset(_connectionString);
        }

        public async Task DisposeAsync()
        {
            SqlConnection?.Dispose();
            await new Respawn.Checkpoint().Reset(_connectionString);
        }
    }
}
