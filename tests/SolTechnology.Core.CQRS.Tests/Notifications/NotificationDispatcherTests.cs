using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.CQRS.Tests.Notifications;

[TestFixture]
public class NotificationDispatcherTests
{
    private IMediator _sut = null!;
    private ServiceProvider _sp = null!;
    private NotificationLog _log = null!;

    [SetUp]
    public void Setup()
    {
        _log = new NotificationLog();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_log);
        services.AddSolCQRS(o => o
            .RegisterCommandsFromAssembly(typeof(NotificationDispatcherTests).Assembly)
            .RegisterQueriesFromAssembly(typeof(NotificationDispatcherTests).Assembly)
            .RegisterEventsFromAssembly(typeof(NotificationDispatcherTests).Assembly));
        _sp = services.BuildServiceProvider();
        _sut = _sp.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
    }

    [TearDown]
    public void TearDown() => _sp?.Dispose();

    [Test]
    public void Publish_ReturnsImmediately()
    {
        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _sut.Publish(new TestNotification { Message = "fast" });
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Test]
    public async Task Publish_AllHandlersAreInvoked()
    {
        // Act
        _sut.Publish(new TestNotification { Message = "hello" });

        // Give background tasks time to complete
        await Task.Delay(1000);

        // Assert
        _log.Messages.Should().Contain("hello");
    }

    [Test]
    public async Task Publish_ThrowingHandler_DoesNotPreventSiblingHandlers()
    {
        // Act
        _sut.Publish(new TestNotification { Message = "resilient" });

        // Give background tasks time to complete
        await Task.Delay(1000);

        // Assert
        _log.Messages.Should().Contain("resilient");
    }

    [Test]
    public void Publish_ThrowingHandler_DoesNotThrowToCaller()
    {
        // Act & Assert
        var act = () => _sut.Publish(new TestNotification { Message = "no-throw" });
        act.Should().NotThrow();
    }

    [Test]
    public async Task Dispatch_HandlerWithExplicitInterfaceImpl_IsInvoked()
    {
        // Arrange
        await using var scope = _sp.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();

        // Act
        await dispatcher.Dispatch(new ExplicitHandlerEvent { Message = "explicit-impl" }, CancellationToken.None);

        // Assert
        _log.Messages.Should().Contain("explicit-impl");
    }

    [Test]
    public async Task Dispatch_HandlerThrowsSynchronously_LogsOriginalException()
    {
        // Arrange
        var loggerProvider = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(loggerProvider));
        services.AddSingleton(_log);
        services.AddSolCQRS(o => o.RegisterEventsFromAssembly(typeof(NotificationDispatcherTests).Assembly));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();

        // Act
        await dispatcher.Dispatch(new TestNotification { Message = "sibling" }, CancellationToken.None);

        // Assert
        _log.Messages.Should().Contain("sibling");
        var failure = loggerProvider.Logs.Should().ContainSingle(l => l.Level == LogLevel.Error).Subject;
        failure.Exception.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("Handler exploded");
    }
}
