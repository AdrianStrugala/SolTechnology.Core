namespace SolTechnology.Core.CQRS;

/// <summary>
/// Marker for a query that returns <typeparamref name="TResult"/>.
/// Handler returns <see cref="Result{TResult}"/>. Queries must be side-effect-free.
/// </summary>
public interface IQuery<TResult> { }

