using System.Data;

namespace SolTechnology.Core.Sql.Connection
{
    public interface ISqlConnectionFactory
    {
        IDbConnection CreateConnection();

        string GetConnectionString();
    }
}