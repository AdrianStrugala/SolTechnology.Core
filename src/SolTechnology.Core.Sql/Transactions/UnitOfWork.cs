using System.Transactions;

namespace SolTechnology.Core.Sql.Transactions
{
    public class UnitOfWork : IDisposable, IUnitOfWork
    {
        readonly TransactionScope _scope = new();

        public void Complete()
        {
            _scope.Complete();
            _scope.Dispose();
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
