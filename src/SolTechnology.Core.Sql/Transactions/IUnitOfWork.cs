namespace SolTechnology.Core.Sql.Transactions;

public interface IUnitOfWork
{
    void Complete();
    void Rollback();
}