using System.Data;

namespace SolTechnology.Core.SQL.Connections
{
    public interface ISQLConnectionFactory
    {
        IDbConnection CreateConnection();

        string GetConnectionString();
    }
}