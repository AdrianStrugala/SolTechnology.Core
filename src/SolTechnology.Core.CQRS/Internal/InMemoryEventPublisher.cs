using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.CQRS.Internal;

/// <summary>
/// Default <see cref="IEventPublisher"/>. Fire-and-forget dispatch in a fresh DI scope, under its own Chronicle root
/// (<c>Event {EventType}</c>) when a Chronicle is registered, because the dispatch usually outlives the publisher.
/// </summary>
internal sealed class InMemoryEventPublisher(
    IServiceScopeFactory scopeFactory,
    ILogger<InMemoryEventPublisher> logger,
    IChronicle? chronicle = null) : IEventPublisher
{
    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        DispatchFireAndForget(@event);
    }

    public void Publish(IEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        DispatchFireAndForget(@event);
    }

    private void DispatchFireAndForget(IEvent @event)
    {
        _ = Task.Run(async () =>
        {
            using var root = chronicle?.BeginRoot($"Event {@event.GetType().Name}") ?? default;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
                await dispatcher.Dispatch(@event, CancellationToken.None);
            }
            catch (Exception ex)
            {
                root.Fail(ex);
                logger.LogError(ex, "Failed to dispatch event [{EventType}]",
                    @event.GetType().Name);
            }
        });
    }
}

