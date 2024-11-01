using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Sql.Connections;
using Testcontainers.MsSql;
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
                .AddJsonFile("appsettings.development.json", true, true)
                .AddJsonFile("appsettings.tests.json", true, true)
                .Build();

            var sqlConfiguration = configuration.GetRequiredSection("Configuration:Sql").Get<SqlConfiguration>();
            var options = Options.Create(sqlConfiguration);
            _connectionString = sqlConfiguration!.ConnectionString;

            SqlConnection?.Dispose();
            SqlConnection = new SqlConnection(_connectionString);

            var canConnect = await CanConnect();
            if (!canConnect)
            {
                var parsedConnectionString = ConnectionStringParser.Parse(_connectionString);
                var container = CreateSqlContainer(parsedConnectionString);
                await container.StartAsync();
                await container.ExecScriptAsync($"create database [{parsedConnectionString["Database"]}]");

                canConnect = await CanConnect();
                if (!canConnect)
                {
                    throw new Exception($"Unable to connect to Sql Server. Connection string is: {_connectionString}");
                }
            }


            SqlConnectionFactory = new SqlConnectionFactory(options!);

            await new Respawn.Checkpoint().Reset(_connectionString);
        }

        public async Task DisposeAsync()
        {
            SqlConnection?.Dispose();
            await new Respawn.Checkpoint().Reset(_connectionString);
        }


        private async Task<bool> CanConnect()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedMilliseconds < 5000)
            {
                try
                {
                    SqlConnection!.Open();
                    return true;
                }
                catch (Exception)
                {
                    await Task.Delay(200);
                }
            }
            return false;
        }

        private MsSqlContainer CreateSqlContainer(Dictionary<string, string> parsedConnectionString)
        {
            //TODO: this is throwing with respawn. To check

            var mssqlContainer = new MsSqlBuilder()
                .WithPassword(parsedConnectionString["Password"])
                .WithPortBinding(int.Parse(parsedConnectionString["Port"]), 1433)
                .WithCleanUp(true)
                .Build();

            return mssqlContainer;
        }
    }

}
