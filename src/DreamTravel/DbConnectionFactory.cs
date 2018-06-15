using System.Data;
using System.Data.SqlClient;

namespace DreamTravel
{
    public class DbConnectionFactory
    {
        public string ConnectionString { get; set; }

        public IDbConnection CreateDbConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}
