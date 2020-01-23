using System.Data;

namespace SolTechnology.Database.Connection
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}