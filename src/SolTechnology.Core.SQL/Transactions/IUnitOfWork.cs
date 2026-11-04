using System.Transactions;

namespace SolTechnology.Core.SQL.Transactions;

public interface IUnitOfWork : IDisposable
{
    IUnitOfWork Begin(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    void Complete();
    void Rollback();
}
