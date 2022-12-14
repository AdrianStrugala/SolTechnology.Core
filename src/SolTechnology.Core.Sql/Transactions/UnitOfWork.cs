using System.Transactions;

namespace SolTechnology.Core.Sql.Transactions
{
    public class UnitOfWork : IDisposable, IUnitOfWork
    {
        private readonly TransactionScope _scope;

        public UnitOfWork(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = isolationLevel },
                TransactionScopeAsyncFlowOption.Enabled);
        }

        public void Complete()
        {
            _scope.Complete();
            Dispose();
        }

        public void Rollback()
        {
            _scope.Dispose();
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}
