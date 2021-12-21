using System.Data;

namespace SolTechnology.Database.Connection
{
    public interface ISqlConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}