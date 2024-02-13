namespace SolTechnology.Core.CQRS.Operations
{
    public abstract class OperationQueryHandlerBase<TQuery, TResult, TContext> : IQueryHandler<TQuery, TResult>
    {
    
        public TContext Context;

        public abstract void Start(TQuery query);
        public abstract IEnumerable<Func<TContext, Task<OperationResult>>> OperationsOrder();
        public abstract Task<TResult> End();


        public Task<TResult> Handle(TQuery query)
        {
            Start(query);
            foreach (var orderedOperation in OperationsOrder())
            {
                orderedOperation.Invoke(Context);
            }

            return End();
        }
    }
}
