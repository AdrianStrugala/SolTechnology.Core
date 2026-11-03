namespace SolTechnology.Core.CQRS;

/// <summary>
/// Handles events dispatched via <see cref="IMediator.Publish{TEvent}"/>.
/// Handlers run sequentially; a throwing handler is logged and never stops siblings.
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>Reacts to the event. An exception is logged by the dispatcher and does not reach the publisher.</summary>
    Task Handle(TEvent notification, CancellationToken cancellationToken);
}



