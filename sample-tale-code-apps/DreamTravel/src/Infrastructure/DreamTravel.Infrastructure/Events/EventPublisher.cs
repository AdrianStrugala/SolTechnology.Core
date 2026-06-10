using Hangfire;
using SolTechnology.Core.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Infrastructure.Events;

public interface IHangfireNotificationPublisher
{
    void Publish(IEvent notification);
    void DispatchEvent(IEvent notification);
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

    public void Publish(IEvent notification)
    {
        _backgroundJobClient.Enqueue(() => DispatchEvent(notification));
    }


    [Hangfire.AutomaticRetry(Attempts = 0)]
    public void DispatchEvent(IEvent notification)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        mediator.Publish(notification);
    }

}
