namespace SolTechnology.Core.CQRS;

/// <summary>
/// Dispatches commands, queries, and events through the CQRS pipeline.
/// Overload resolution on marker types (<see cref="ICommand"/>, <see cref="IQuery{TResult}"/>,
/// <see cref="IEvent"/>) distinguishes intent at compile time.
/// </summary>
public interface IMediator
{
    /// <summary>Send a command that produces no data.</summary>
    Task<Result> Send(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>Send a command that produces a result.</summary>
    Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    /// <summary>Send a side-effect-free query.</summary>
    Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fire-and-forget dispatch to every registered <see cref="IEventHandler{T}"/>.
    /// Returns immediately. Every handler runs on its own background task with a fresh DI scope.
    /// Failures are isolated and logged — they never propagate to the caller and never stop other handlers.
    /// </summary>
    void Publish<TEvent>(TEvent notification) where TEvent : IEvent;

    /// <summary>
    /// Non-generic overload for runtime dispatch when the concrete event type is not
    /// known at compile time (e.g. deserialized from a queue).
    /// </summary>
    void Publish(IEvent notification);
}


