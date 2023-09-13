namespace SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository;

public interface IExecutionErrorRepository
{
    void Insert(ExecutionError executionError);
    List<ExecutionError> GetByReferenceType(ReferenceType referenceType);
}