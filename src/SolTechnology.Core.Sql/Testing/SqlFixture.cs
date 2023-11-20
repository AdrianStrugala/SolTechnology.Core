using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Sql.Connections;
using Xunit;
using SqlConnection = System.Data.SqlClient.SqlConnection;

namespace SolTechnology.Core.Sql.Testing
{
    public class SqlFixture : IAsyncLifetime
    {
        public ISqlConnectionFactory SqlConnectionFactory = null!;
        public SqlConnection? SqlConnection { get; private set; }
        private string _connectionString = null!;

        public async Task InitializeAsync()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.local.json", true, true)
                .AddJsonFile("appsettings.functional.tests.json", true, true)
                .Build();

            var sqlConfiguration = configuration.GetRequiredSection("Configuration:Sql").Get<SqlConfiguration>();
            var options = Options.Create(sqlConfiguration);

            SqlConnectionFactory = new SqlConnectionFactory(options!);
            _connectionString = sqlConfiguration!.ConnectionString;

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
