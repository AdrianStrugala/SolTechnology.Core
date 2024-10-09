using Hangfire;
using MediatR;

namespace DreamTravel.Infrastructure.Events;

public interface IHangfireNotificationPublisher
{
    void Publish(INotification notification);
    void DispatchEvent(INotification notification);
}

public class HangfireNotificationPublisher : IHangfireNotificationPublisher
{
    private readonly IMediator _mediator;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireNotificationPublisher(IMediator mediator, IBackgroundJobClient backgroundJobClient)
    {
        _mediator = mediator;
        _backgroundJobClient = backgroundJobClient;
    }

    public void Publish(INotification notification)
    {
        _backgroundJobClient.Enqueue(() => DispatchEvent(notification));
    }


    public void DispatchEvent(INotification notification)
    {
        _mediator.Publish(notification);
    }

}