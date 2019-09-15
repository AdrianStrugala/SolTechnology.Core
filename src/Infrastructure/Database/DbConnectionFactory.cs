using System.Data;
using System.Data.SqlClient;

namespace DreamTravel.Infrastructure.Database
{
    public static class DbConnectionFactory
    {
        private static readonly string ConnectionString;


        static DbConnectionFactory()
        {
            var applicationConfiguration = new ApplicationConfiguration();
            ConnectionString = applicationConfiguration.ConnectionString;
        }

        public static IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }
    }
}
