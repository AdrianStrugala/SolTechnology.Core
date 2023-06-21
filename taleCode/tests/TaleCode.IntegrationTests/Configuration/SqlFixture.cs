using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Sql;
using SolTechnology.Core.Sql.Connection;
using Xunit;
using SqlConnection = System.Data.SqlClient.SqlConnection;

namespace TaleCode.IntegrationTests.Sql.Configuration
{
    public class SqlFixture : IAsyncLifetime
    {
        public ISqlConnectionFactory SqlConnectionFactory;
        public SqlConnection SqlConnection { get; private set; }
        private string _connectionString;

        public async Task InitializeAsync()
        {
            var settingsFile = File.ReadAllText("appsettings.functional.tests.json");
            _connectionString = JsonDocument.Parse(settingsFile)
                .RootElement
                .GetProperty("Configuration")
                .GetProperty("Sql")
                .GetProperty("ConnectionString")
                .GetString()!;

            var config = Options.Create(new SqlConfiguration
            {
                ConnectionString = _connectionString
            });

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
