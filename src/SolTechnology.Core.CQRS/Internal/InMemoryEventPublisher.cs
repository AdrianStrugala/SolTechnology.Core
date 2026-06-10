using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.CQRS.Internal;

/// <summary>
/// Default <see cref="IEventPublisher"/>. Fire-and-forget dispatch in a fresh DI scope.
/// </summary>
internal sealed class InMemoryEventPublisher(IServiceScopeFactory scopeFactory, ILogger<InMemoryEventPublisher> logger) : IEventPublisher
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
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
                await dispatcher.Dispatch(@event, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch event [{EventType}]",
                    @event.GetType().Name);
            }
        });
    }
}

