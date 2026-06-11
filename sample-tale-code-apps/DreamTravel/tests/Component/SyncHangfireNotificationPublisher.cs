using SolTechnology.Core.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.FunctionalTests;

/// <summary>
/// Test-only synchronous <see cref="IEventPublisher"/> that dispatches events in-process.
/// Resolves <see cref="IEventDispatcher"/> from the Worker host's scope so handlers complete
/// before <c>Publish</c> returns — deterministic for assertions.
/// </summary>
internal sealed class SyncEventPublisher : IEventPublisher
{
    private static Func<IServiceScopeFactory>? _workerScopeFactoryAccessor;

    public static void UseScopeFactory(Func<IServiceScopeFactory> accessor)
        => _workerScopeFactoryAccessor = accessor;

    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        Dispatch(@event);
    }

    public void Publish(IEvent @event)
    {
        Dispatch(@event);
    }

    private void Dispatch(IEvent @event)
    {
        var factory = _workerScopeFactoryAccessor?.Invoke()
            ?? throw new InvalidOperationException(
                $"{nameof(SyncEventPublisher)}.{nameof(UseScopeFactory)} must be called " +
                "in the test fixture before publishing.");

        using var scope = factory.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
        dispatcher.Dispatch(@event, CancellationToken.None).GetAwaiter().GetResult();
    }
}
