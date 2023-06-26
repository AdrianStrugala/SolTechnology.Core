using System.Data;

namespace SolTechnology.Core.Sql.Connections
{
    public interface ISqlConnectionFactory
    {
        IDbConnection CreateConnection();

        string GetConnectionString();
    }
}