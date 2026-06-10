using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// <see cref="IEventPublisher"/> that enqueues one Hangfire background job per event.
/// Requires app-owned <c>AddHangfire</c>/<c>AddHangfireServer</c> with a DI-aware activator
/// and <c>UseRecommendedSerializerSettings()</c> for interface-typed argument round-tripping.
/// </summary>
internal sealed class HangfireEventPublisher(
    IBackgroundJobClient backgroundJobClient,
    IServiceScopeFactory scopeFactory,
    IOptions<PersistentEventsOptions> options) : IEventPublisher
{
    private readonly string _queueName = options.Value.QueueName;

    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        Enqueue(@event);
    }

    public void Publish(IEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        Enqueue(@event);
    }

    [AutomaticRetry(Attempts = 0)]
    public void DispatchInScope(IEvent @event)
    {
        using var scope = scopeFactory.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
        dispatcher.Dispatch(@event, CancellationToken.None).GetAwaiter().GetResult();
    }

    private void Enqueue(IEvent @event)
    {
        backgroundJobClient.Enqueue<HangfireEventPublisher>(
            _queueName,
            publisher => publisher.DispatchInScope(@event));
    }
}

