namespace SolTechnology.Core.CQRS;

/// <summary>
/// Fans an event out to every registered <see cref="IEventHandler{T}"/>.
/// Handlers run sequentially; failures are isolated and never stop siblings.
/// </summary>
public interface IEventDispatcher
{
    /// <summary>Dispatch the event to all handlers registered for its runtime type.</summary>
    Task Dispatch(IEvent @event, CancellationToken cancellationToken);
}



