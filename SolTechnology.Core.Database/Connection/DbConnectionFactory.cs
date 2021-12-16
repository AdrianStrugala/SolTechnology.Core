using System;
using System.Data;
using System.Data.SqlClient;
using Polly;

namespace SolTechnology.Database.Connection
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        private readonly Random _random = new Random();

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);

            Policy.Handle<Exception>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)) // 3,9,27s
                               + TimeSpan.FromMilliseconds(_random.Next(1000))) //delay up to 1s
                .Execute(() =>
                  {
                      connection.Open();
                  });

            return connection;
        }

    }
}
