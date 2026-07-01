using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
}
