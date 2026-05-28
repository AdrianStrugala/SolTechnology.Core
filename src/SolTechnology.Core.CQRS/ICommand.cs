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

/// <summary>
/// Marker for a query that returns <typeparamref name="TResult"/>.
/// Handler returns <see cref="Result{TResult}"/>. Queries must be side-effect-free.
/// </summary>
public interface IQuery<TResult> { }

/// <summary>
/// Marker for a notification dispatched fire-and-forget to all registered handlers.
/// </summary>
public interface INotification { }

