using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.CQRS.Internal;

/// <summary>
/// Default <see cref="IEventDispatcher"/>. Sequential fan-out with per-handler error isolation; a cancellation of the
/// caller's token stops the fan-out and propagates.
/// Handlers are called through <see cref="IEventHandler{TEvent}"/> by a delegate cached per event type, each inside
/// its own Chronicle step when a Chronicle is registered.
/// </summary>
internal sealed class EventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<EventDispatcher> logger,
    IChronicle? chronicle = null) : IEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Func<object, IEvent, CancellationToken, Task>> Invokers = new();

    private static readonly MethodInfo InvokeDefinition =
        typeof(EventDispatcher).GetMethod(nameof(InvokeHandler), BindingFlags.NonPublic | BindingFlags.Static)!;

    public async Task Dispatch(IEvent @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = @event.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handlers = serviceProvider.GetServices(handlerType).Cast<object>().ToList();
        var invoke = Invokers.GetOrAdd(
            eventType,
            static type => InvokeDefinition.MakeGenericMethod(type).CreateDelegate<Func<object, IEvent, CancellationToken, Task>>());

        foreach (var handler in handlers)
        {
            using var step = chronicle?.Step($"Event {eventType.Name} {handler.GetType().Name}") ?? default;
            try
            {
                await invoke(handler, @event, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                step.Cancel();
                throw;
            }
            catch (Exception ex)
            {
                step.Fail(ex);
                logger.LogError(ex, "Event handler [{HandlerType}] failed for [{EventType}]",
                    handler.GetType().Name, eventType.Name);
            }
        }
    }

    private static Task InvokeHandler<TEvent>(object handler, IEvent @event, CancellationToken cancellationToken)
        where TEvent : IEvent
        => ((IEventHandler<TEvent>)handler).Handle((TEvent)@event, cancellationToken);
}
