using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.CQRS.Internal;

/// <summary>
/// Default <see cref="IEventDispatcher"/>. Sequential fan-out with per-handler error isolation.
/// </summary>
internal sealed class EventDispatcher(IServiceProvider serviceProvider, ILogger<EventDispatcher> logger) : IEventDispatcher
{
    public async Task Dispatch(IEvent @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = @event.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handlers = serviceProvider.GetServices(handlerType).Cast<object>().ToList();

        foreach (var handler in handlers)
        {
            try
            {
                var handleMethod = handler.GetType().GetMethod("Handle", new[] { eventType, typeof(CancellationToken) })!;
                var task = (Task)handleMethod.Invoke(handler, new object[] { @event, cancellationToken })!;
                await task;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Event handler [{HandlerType}] failed for [{EventType}]",
                    handler.GetType().Name, eventType.Name);
            }
        }
    }
}



