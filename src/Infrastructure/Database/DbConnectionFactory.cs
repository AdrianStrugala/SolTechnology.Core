using System;
using System.Data;
using System.Data.SqlClient;
using Polly;

namespace DreamTravel.Infrastructure.Database
{
    public static class DbConnectionFactory
    {
        private static readonly string ConnectionString;
        private static readonly Random Random = new Random();

        static DbConnectionFactory()
        {
            var applicationConfiguration = new ApplicationConfiguration();
            ConnectionString = applicationConfiguration.ConnectionString;
        }

            public static IDbConnection CreateConnection()
            {
                var connection = new SqlConnection(ConnectionString);

                Policy.Handle<Exception>()
                            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)) // 3,9,27s
                                                                  + TimeSpan.FromMilliseconds(Random.Next(1000))) //delay up to 1s
                            .Execute(() =>
                                          {
                                              connection.Open();
                                          });

                return connection;
            }

    }
}
