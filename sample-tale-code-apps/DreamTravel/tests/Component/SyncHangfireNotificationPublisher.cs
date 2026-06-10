using DreamTravel.Infrastructure.Events;
using SolTechnology.Core.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.FunctionalTests;

/// <summary>
/// Test-only synchronous replacement for <see cref="IHangfireNotificationPublisher"/>.
/// Dispatches the notification through IMediator resolved from the Worker host's scope.
/// </summary>
internal sealed class SyncHangfireNotificationPublisher : IHangfireNotificationPublisher
{
    private static Func<IServiceScopeFactory>? _workerScopeFactoryAccessor;

    public static void UseScopeFactory(Func<IServiceScopeFactory> accessor)
        => _workerScopeFactoryAccessor = accessor;

    public void Publish(IEvent notification) => DispatchEvent(notification);

    public void DispatchEvent(IEvent notification)
    {
        var factory = _workerScopeFactoryAccessor?.Invoke()
            ?? throw new InvalidOperationException(
                $"{nameof(SyncHangfireNotificationPublisher)}.{nameof(UseScopeFactory)} must be called " +
                "in the test fixture before publishing.");

        using var scope = factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        mediator.Publish(notification);
    }
}
