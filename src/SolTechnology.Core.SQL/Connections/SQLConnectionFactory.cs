using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Polly;

namespace SolTechnology.Core.SQL.Connections
{
    internal sealed class SQLConnectionFactory : ISQLConnectionFactory
    {
        private readonly string _connectionString;
        private readonly Func<int, TimeSpan> _retryDelay;

        public SQLConnectionFactory(IOptions<SQLConfiguration> sqlConfiguration)
            : this(sqlConfiguration, BackoffWithJitter)
        {
        }

        internal SQLConnectionFactory(IOptions<SQLConfiguration> sqlConfiguration, Func<int, TimeSpan> retryDelay)
        {
            _connectionString = sqlConfiguration.Value.ConnectionString;
            _retryDelay = retryDelay;
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);

            try
            {
                Policy.Handle<SqlException>(SQLTransientErrors.IsTransient)
                    .WaitAndRetry(3, _retryDelay)
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

        private static TimeSpan BackoffWithJitter(int retryAttempt) =>
            TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(1000));
    }
}
