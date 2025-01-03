using System.Diagnostics;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Sql.Connections;
using Testcontainers.MsSql;
using SqlConnection = System.Data.SqlClient.SqlConnection;

namespace SolTechnology.Core.Sql.Testing
{
    public class SqlFixture : IDisposable
    {
        public ISqlConnectionFactory SqlConnectionFactory = null!;
        public SqlConnection? SqlConnection { get; private set; }
        private string _connectionString = null!;

        public async Task Connect(SqlConfiguration sqlConfiguration)
        {
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
        }
        
        public async Task Reset()
        {
            await new Respawn.Checkpoint().Reset(_connectionString);
        }

        public void Dispose()
        {
            SqlConnection?.Dispose();
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
