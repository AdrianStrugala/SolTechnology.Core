namespace SolTechnology.Core.CQRS;

/// <summary>
/// Marker for a command that produces no meaningful data (side-effect only).
/// Handler returns <see cref="Result"/>.
/// </summary>
public interface ICommand : ICommand<Nothing> { }

/// <summary>
/// Marker for a command that produces a result of type <typeparamref name="TResult"/>.
/// Handler returns <see cref="Result{TResult}"/>.
/// </summary>
public interface ICommand<TResult> { }

