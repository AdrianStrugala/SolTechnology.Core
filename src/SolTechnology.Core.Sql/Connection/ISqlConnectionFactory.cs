using System.Data;

namespace SolTechnology.Core.Sql.Connection
{
    public interface ISqlConnectionFactory
    {
        IDbConnection CreateConnection();

        string GetConnectionString();

        public bool HasOpenTransaction { get; }
        void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        IDbTransaction GetTransaction();
        void Commit();
        void Rollback();
    }
}