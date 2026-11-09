namespace SolTechnology.Core.CQRS;

/// <summary>
/// Handles events dispatched via <see cref="IMediator.Publish{TEvent}"/>.
/// Handlers run sequentially; a throwing handler is logged and never stops siblings, unless the dispatch is cancelled.
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Reacts to the event. An exception is logged by the dispatcher and does not reach the publisher, except an
    /// <see cref="OperationCanceledException"/> for a cancelled <paramref name="cancellationToken"/>, which stops the dispatch.
    /// </summary>
    Task Handle(TEvent notification, CancellationToken cancellationToken);
}



