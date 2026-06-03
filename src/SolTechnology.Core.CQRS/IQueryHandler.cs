namespace SolTechnology.Core.CQRS;

/// <summary>
/// Handles a query that returns <typeparamref name="TResult"/>.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken);
}
