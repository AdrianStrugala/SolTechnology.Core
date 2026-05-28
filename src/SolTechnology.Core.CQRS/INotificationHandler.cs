namespace SolTechnology.Core.CQRS;

/// <summary>
/// Handles notifications dispatched via <see cref="IMediator.Publish{TNotification}"/>.
/// Each handler runs on its own background task with a fresh DI scope.
/// </summary>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

