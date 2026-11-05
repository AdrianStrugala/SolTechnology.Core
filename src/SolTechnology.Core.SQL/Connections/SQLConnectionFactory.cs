using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Polly;

namespace SolTechnology.Core.SQL.Connections
{
    public class SQLConnectionFactory(IOptions<SQLConfiguration> sqlConfiguration) : ISQLConnectionFactory
    {
        private static readonly HashSet<int> TransientErrorNumbers = [-2, 53, 1205, 4060, 40197, 40501, 40613, 49918, 49919, 49920];

        private readonly string _connectionString = sqlConfiguration.Value.ConnectionString;

        public IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);

            try
            {
                Policy.Handle<SqlException>(exception => TransientErrorNumbers.Contains(exception.Number))
                    .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)) // 3,9,27s
                                   + TimeSpan.FromMilliseconds(Random.Shared.Next(1000))) //delay up to 1s
                    .Execute(() =>
                      {
                          connection.Open();
                      });
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            return connection;
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }
    }
}
