namespace SolTechnology.Core.SQL.Transactions;

public interface IUnitOfWork
{
    void Complete();
    void Rollback();
}