namespace SolTechnology.Core.CQRS;

/// <summary>
/// Handles events dispatched via <see cref="IMediator.Publish{TEvent}"/>.
/// Each handler runs on its own background task with a fresh DI scope.
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task Handle(TEvent notification, CancellationToken cancellationToken);
}

