namespace SolTechnology.Core.CQRS
{
    public interface IQueryHandler<in TQuery, TResult>
    {
        public Task<OperationResult<TResult>> Handle(TQuery query, CancellationToken cancellationToken = default);
    }
}