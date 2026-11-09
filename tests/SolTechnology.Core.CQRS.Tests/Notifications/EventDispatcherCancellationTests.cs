using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.CQRS.Tests.Notifications;

[TestFixture]
public class EventDispatcherCancellationTests
{
    private ConcurrentQueue<string> _handled = null!;
    private RecordingLoggerProvider _logs = null!;
    private CancellationTokenSource _cancellation = null!;

    [SetUp]
    public void Setup()
    {
        _handled = new ConcurrentQueue<string>();
        _logs = new RecordingLoggerProvider();
        _cancellation = new CancellationTokenSource();
    }

    [TearDown]
    public void TearDown()
    {
        _cancellation.Dispose();
        _logs.Dispose();
    }

    [Test]
    public async Task Dispatch_HandlerInterruptedByCancellation_StopsAndPropagates()
    {
        // Arrange
        await using var services = Services<WaitingHandler, RecordingHandler>();
        var dispatching = Dispatch(services);

        // Act
        await _cancellation.CancelAsync();
        var act = () => dispatching.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        _handled.Should().BeEmpty();
        _logs.Logs.Should().NotContain(l => l.Level == LogLevel.Error);
    }

    [Test]
    public async Task Dispatch_AllHandlersCompleteThenTokenCancelled_Completes()
    {
        // Arrange
        await using var services = Services<RecordingHandler, CancelAfterWorkHandler>();

        // Act
        var act = () => Dispatch(services);

        // Assert
        await act.Should().NotThrowAsync();
        _handled.Should().Equal("recorded", "finished");
    }

    [Test]
    public async Task Dispatch_OperationCanceledWithoutCancelledToken_IsLoggedAndSiblingsRun()
    {
        // Arrange
        await using var services = Services<SelfCancellingHandler, RecordingHandler>();

        // Act
        await Dispatch(services);

        // Assert
        _handled.Should().Equal("recorded");
        _logs.Logs.Should().ContainSingle(l => l.Level == LogLevel.Error)
            .Which.Exception.Should().BeAssignableTo<OperationCanceledException>();
    }

    private ServiceProvider Services<TFirst, TSecond>()
        where TFirst : class, IEventHandler<CancellationEvent>
        where TSecond : class, IEventHandler<CancellationEvent>
    {
        var services = new ServiceCollection().AddLogging(b => b.AddProvider(_logs)).AddSolCQRS();
        services.AddSingleton(_handled).AddSingleton(_cancellation);
        services.AddScoped<IEventHandler<CancellationEvent>, TFirst>().AddScoped<IEventHandler<CancellationEvent>, TSecond>();
        return services.BuildServiceProvider();
    }

    private async Task Dispatch(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IEventDispatcher>().Dispatch(new CancellationEvent(), _cancellation.Token);
    }

    public sealed record CancellationEvent : IEvent;

    private sealed class RecordingHandler(ConcurrentQueue<string> handled) : IEventHandler<CancellationEvent>
    {
        public Task Handle(CancellationEvent notification, CancellationToken cancellationToken)
        {
            handled.Enqueue("recorded");
            return Task.CompletedTask;
        }
    }

    private sealed class CancelAfterWorkHandler(ConcurrentQueue<string> handled, CancellationTokenSource cancellation) : IEventHandler<CancellationEvent>
    {
        public Task Handle(CancellationEvent notification, CancellationToken cancellationToken)
        {
            handled.Enqueue("finished");
            return cancellation.CancelAsync();
        }
    }

    private sealed class WaitingHandler : IEventHandler<CancellationEvent>
    {
        public Task Handle(CancellationEvent notification, CancellationToken cancellationToken) =>
            Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private sealed class SelfCancellingHandler : IEventHandler<CancellationEvent>
    {
        public Task Handle(CancellationEvent notification, CancellationToken cancellationToken) =>
            throw new OperationCanceledException("timed out on its own");
    }
}
