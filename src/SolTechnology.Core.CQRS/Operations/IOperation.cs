namespace SolTechnology.Core.CQRS.Operations
{
    public interface IOperation<in TContext>
    {
        public Task<OperationResult> Execute(TContext context);
    }
}
