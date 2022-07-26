using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Options;
using Polly;

namespace SolTechnology.Core.Sql.Connection
{
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;
        private static readonly Random Random = new Random();

        public SqlConnectionFactory(IOptions<SqlConfiguration> sqlConfiguration)
        {
            _connectionString = sqlConfiguration.Value.ConnectionString;
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);

            Policy.Handle<Exception>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)) // 3,9,27s
                               + TimeSpan.FromMilliseconds(Random.Next(1000))) //delay up to 1s
                .Execute(() =>
                  {
                      connection.Open();
                  });

            return connection;
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }
    }
}
