using System.Transactions;

namespace SolTechnology.Core.SQL.Transactions
{
    public class UnitOfWork : IDisposable, IUnitOfWork
    {
        private TransactionScope _scope = null!;

        public UnitOfWork Begin(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = isolationLevel },
                TransactionScopeAsyncFlowOption.Enabled);
            return this;
        }

        public void Complete()
        {
            if (_scope == null)
            {
                throw new InvalidOperationException("The transaction scope was not initialized. Call Begin() at first");
            }

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
