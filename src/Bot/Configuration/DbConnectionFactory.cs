namespace DreamTravel.Bot.Configuration
{
    using System.Data;
    using System.Data.SqlClient;

    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;


        public DbConnectionFactory(ApplicationConfiguration applicationConfiguration)
        {
            _connectionString = applicationConfiguration.ConnectionString;
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
