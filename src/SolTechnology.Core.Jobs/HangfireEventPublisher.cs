using Hangfire;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Jobs;

/// <summary>
/// Interface for publishing MediatR notifications through Hangfire background jobs.
/// </summary>
public interface IHangfireEventPublisher
{
    /// <summary>
    /// Enqueues a notification to be published asynchronously via Hangfire.
    /// </summary>
    void Publish(INotification notification);

    /// <summary>
    /// Dispatches a notification synchronously (called by Hangfire worker).
    /// </summary>
    void DispatchEvent(INotification notification);
}

/// <summary>
/// Publishes MediatR notifications through Hangfire background jobs.
/// Enables fire-and-forget event publishing with automatic persistence and retry support.
/// </summary>
public class HangfireEventPublisher : IHangfireEventPublisher
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireEventPublisher(IServiceScopeFactory serviceScopeFactory, IBackgroundJobClient backgroundJobClient)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <inheritdoc />
    public void Publish(INotification notification)
    {
        _backgroundJobClient.Enqueue(() => DispatchEvent(notification));
    }

    /// <inheritdoc />
    [AutomaticRetry(Attempts = 0)]
    public void DispatchEvent(INotification notification)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        mediator.Publish(notification).GetAwaiter().GetResult();
    }
}
