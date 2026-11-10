using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace SolTechnology.Core.CQRS.Tests.Notifications;

[TestFixture]
public class EventChronicleTests
{
    private ConcurrentQueue<string> _handled = null!;
    private CancellationTokenSource _cancellation = null!;

    [SetUp]
    public void SetUp()
    {
        _handled = new ConcurrentQueue<string>();
        _cancellation = new CancellationTokenSource();
    }

    [TearDown]
    public void TearDown() => _cancellation.Dispose();

    [Test]
    public async Task Publish_InsideOpenRoot_EventGetsOwnRootAndOuterRootHasNoEventStep()
    {
        // Arrange
        using var host = Host(typeof(RecordingHandler));
        var publisher = host.Services.GetRequiredService<IEventPublisher>();

        // Act
        using (host.Chronicle.Begin("CreateTrip"))
        {
            publisher.Publish(new ProbeEvent());
        }

        var reports = await WaitForReports(host, count: 2);

        // Assert
        _handled.Should().Equal("recorded");
        reports.Should().ContainSingle(r => r.Message.StartsWith("Chronicle [CreateTrip] [Ok]"))
            .Which.Message.Should().NotContain("Event ");
        reports.Should().ContainSingle(r => r.Message.StartsWith("Chronicle [Event ProbeEvent] [Ok]"))
            .Which.Message.Should().Contain("`- Event ProbeEvent RecordingHandler [@");
    }

    [TestCase(typeof(FailingHandler), "InvalidOperationException")]
    [TestCase(typeof(SelfCancellingHandler), "OperationCanceledException")]
    public async Task Dispatch_HandlerFails_StepFailedAndOtherHandlersRun(Type failingHandler, string errorType)
    {
        // Arrange
        using var host = Host(failingHandler, typeof(RecordingHandler));

        // Act
        using (host.Chronicle.Begin("Dispatch"))
        {
            await Dispatch(host);
        }

        // Assert
        _handled.Should().Equal("recorded");
        var report = host.Reports.Should().ContainSingle().Which;
        report.Message.Should().Contain($"Event ProbeEvent {failingHandler.Name} [@").And.Contain($"[failed] Error=[{errorType}")
            .And.Contain("Event ProbeEvent RecordingHandler [@").And.Contain("[ok]");
        host.Logs.GetSnapshot().Should().ContainSingle(r => r.Level == LogLevel.Error && r.Id.Id != 2154);
    }

    [Test]
    public async Task Dispatch_HandlerCancelledWithCancelledToken_StepCancelledRethrowsAndLaterHandlersHaveNoStep()
    {
        // Arrange
        using var host = Host(typeof(WaitingHandler), typeof(RecordingHandler));

        // Act
        Func<Task> act;
        using (host.Chronicle.Begin("Dispatch"))
        {
            var dispatching = Dispatch(host);
            await _cancellation.CancelAsync();
            act = () => dispatching.WaitAsync(TimeSpan.FromSeconds(5));
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        // Assert
        _handled.Should().BeEmpty();
        var report = host.Reports.Should().ContainSingle().Which;
        report.Message.Should().Contain("Event ProbeEvent WaitingHandler [@").And.Contain("[cancelled]")
            .And.NotContain("RecordingHandler");
    }

    private ChronicleTestHost Host(params Type[] handlers) => new(services =>
    {
        services.AddSolCQRS();
        services.AddSingleton(_handled);
        foreach (var handler in handlers)
        {
            services.AddScoped(typeof(IEventHandler<ProbeEvent>), handler);
        }
    });

    private async Task Dispatch(ChronicleTestHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IEventDispatcher>().Dispatch(new ProbeEvent(), _cancellation.Token);
    }

    private static async Task<IReadOnlyList<FakeLogRecord>> WaitForReports(ChronicleTestHost host, int count)
    {
        for (var attempt = 0; attempt < 100 && host.Reports.Count < count; attempt++)
        {
            await Task.Delay(50);
        }

        return host.Reports;
    }

    public sealed record ProbeEvent : IEvent;

    private sealed class RecordingHandler(ConcurrentQueue<string> handled) : IEventHandler<ProbeEvent>
    {
        public Task Handle(ProbeEvent notification, CancellationToken cancellationToken)
        {
            handled.Enqueue("recorded");
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler : IEventHandler<ProbeEvent>
    {
        public Task Handle(ProbeEvent notification, CancellationToken cancellationToken) => throw new InvalidOperationException("boom");
    }

    private sealed class SelfCancellingHandler : IEventHandler<ProbeEvent>
    {
        public Task Handle(ProbeEvent notification, CancellationToken cancellationToken) =>
            throw new OperationCanceledException("timed out on its own");
    }

    private sealed class WaitingHandler : IEventHandler<ProbeEvent>
    {
        public Task Handle(ProbeEvent notification, CancellationToken cancellationToken) =>
            Task.Delay(Timeout.Infinite, cancellationToken);
    }
}
