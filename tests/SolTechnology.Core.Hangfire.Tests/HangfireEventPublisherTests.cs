using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Hangfire;

namespace SolTechnology.Core.Hangfire.Tests;

[TestFixture]
public class HangfireEventPublisherTests
{
    private IBackgroundJobClient _jobClient = null!;
    private HangfireEventPublisher _sut = null!;
    private IState? _capturedState;

    [SetUp]
    public void Setup()
    {
        _capturedState = null;
        _jobClient = Substitute.For<IBackgroundJobClient>();
        _jobClient.Create(Arg.Any<Job>(), Arg.Do<IState>(s => _capturedState = s)).Returns("job-1");

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = Options.Create(new PersistentEventsOptions { QueueName = "events" });

        _sut = new HangfireEventPublisher(_jobClient, scopeFactory, options);
    }

    [Test]
    public void Publish_EnqueuesExactlyOneJob()
    {
        // Arrange
        var @event = new TestEvent("hello");

        // Act
        _sut.Publish(@event);

        // Assert
        _jobClient.Received(1).Create(Arg.Any<Job>(), Arg.Any<IState>());
    }

    [Test]
    public void PersistentEventsOptions_DefaultQueueName_IsDefault()
    {
        // Assert
        new PersistentEventsOptions().QueueName.Should().Be("default");
    }

    [Test]
    public void Publish_NullEvent_Throws()
    {
        // Act
        var act = () => _sut.Publish((IEvent)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void DispatchInScope_ResolvesDispatcherFromFreshScope()
    {
        // Arrange
        var dispatcher = Substitute.For<IEventDispatcher>();
        dispatcher.Dispatch(Arg.Any<IEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var scopedSp = Substitute.For<IServiceProvider>();
        scopedSp.GetService(typeof(IEventDispatcher)).Returns(dispatcher);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedSp);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var options = Options.Create(new PersistentEventsOptions());
        var sut = new HangfireEventPublisher(_jobClient, scopeFactory, options);

        var @event = new TestEvent("dispatch-test");

        // Act
        sut.DispatchInScope(@event);

        // Assert
        dispatcher.Received(1).Dispatch(@event, Arg.Any<CancellationToken>());
        scopeFactory.Received(1).CreateScope();
    }
}

public record TestEvent(string Message) : IEvent;








