namespace SolTechnology.Core.CQRS
{
    public interface IQueryHandler<in TQuery, TResult>
    {
        public Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
    }
}