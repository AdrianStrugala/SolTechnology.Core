namespace SolTechnology.Core.CQRS;

/// <summary>
/// Handles a query that returns <typeparamref name="TResult"/>.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    /// <summary>Answers the query without side effects, or returns a failed <see cref="Result{TResult}"/>.</summary>
    Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken);
}
