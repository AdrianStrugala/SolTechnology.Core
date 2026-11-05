using System.Transactions;

namespace SolTechnology.Core.SQL.Transactions
{
    internal sealed class UnitOfWork : IDisposable, IUnitOfWork
    {
        private TransactionScope? _scope;

        public IUnitOfWork Begin(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_scope != null)
            {
                throw new InvalidOperationException("The transaction scope is already open. Complete or roll it back before calling Begin() again");
            }

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
            Dispose();
        }

        public void Dispose()
        {
            _scope?.Dispose();
            _scope = null;
        }
    }
}
