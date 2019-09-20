using System.Threading.Tasks;
using DreamTravel.Infrastructure.Database;
using Xunit;

namespace DreamTravel.DatabaseDataTests
{
    public class SqlFixture : IAsyncLifetime
    {
        public IDbConnectionFactory DbConnectionFactory;
        private string _connectionString;

        public async Task InitializeAsync()
        {
            _connectionString =
                "Server=tcp:dreamtravel.database.windows.net,1433;Initial Catalog=dreamtravel-demo;Persist Security Info=False;User ID=adrian;Password=P4ssw0rd@;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;";
            DbConnectionFactory = new DbConnectionFactory(_connectionString);

            await new Respawn.Checkpoint().Reset(_connectionString);
        }

        public async Task DisposeAsync()
        {
            await new Respawn.Checkpoint().Reset(_connectionString);
        }
    }
}
