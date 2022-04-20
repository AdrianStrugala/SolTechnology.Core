using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Sql;
using SolTechnology.Core.Sql.Connection;
using Xunit;
using SqlConnection = System.Data.SqlClient.SqlConnection;

namespace TaleCode.IntegrationTests.SqlData
{
    public class SqlFixture : IAsyncLifetime
    {
        public ISqlConnectionFactory SqlConnectionFactory;
        public SqlConnection SqlConnection { get; private set; }
        private string _connectionString;

        public async Task InitializeAsync()
        {
            var config = Options.Create(new SqlConfiguration
            {
                ConnectionString =
                    "Data Source=localhost,1401;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
            });

            _connectionString = config.Value.ConnectionString;

            SqlConnectionFactory = new SqlConnectionFactory(config);

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
