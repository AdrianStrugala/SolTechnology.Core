using Hangfire;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Infrastructure.Events;

public interface IHangfireNotificationPublisher
{
    void Publish(INotification notification);
    void DispatchEvent(INotification notification);
}

public class HangfireNotificationPublisher : IHangfireNotificationPublisher
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireNotificationPublisher(IServiceScopeFactory serviceScopeFactory, IBackgroundJobClient backgroundJobClient)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _backgroundJobClient = backgroundJobClient;
    }

    public void Publish(INotification notification)
    {
        _backgroundJobClient.Enqueue(() => DispatchEvent(notification));
    }


    [Hangfire.AutomaticRetry(Attempts = 0)] // Optional: prevent retries if not needed
    public void DispatchEvent(INotification notification)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        mediator.Publish(notification).GetAwaiter().GetResult();
    }

}