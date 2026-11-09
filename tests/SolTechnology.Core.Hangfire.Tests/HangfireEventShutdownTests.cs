using System.Collections.Concurrent;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.Hangfire.Tests;

/// <summary>
/// Event jobs performed by Hangfire with CQRS's real dispatcher when the server shuts down: a finished dispatch
/// succeeds, an interrupted one aborts so Hangfire requeues it.
/// </summary>
[TestFixture]
public class HangfireEventShutdownTests
{
    private ConcurrentQueue<string> _handled = null!;
    private CancellationTokenSource _shutdown = null!;

    [SetUp]
    public void Setup()
    {
        _handled = new ConcurrentQueue<string>();
        _shutdown = new CancellationTokenSource();
    }

    [TearDown]
    public void TearDown() => _shutdown.Dispose();

    [Test]
    public void DispatchInScope_HandlersFinishBeforeShutdown_JobSucceeds()
    {
        // Arrange
        var job = Job.FromExpression<HangfireEventPublisher>(p => p.DispatchInScope(new ShutdownEvent(), CancellationToken.None));
        using var services = Services<RecordingHandler, ShutdownAfterWorkHandler>();

        // Act
        var act = () => Perform(services, job);

        // Assert
        act.Should().NotThrow();
        _handled.Should().Equal("recorded", "finished");
    }

    [Test]
    public async Task DispatchInScope_HandlerInterruptedByShutdown_JobRequeuesAndSkipsRemainingHandlers()
    {
        // Arrange
        var job = Job.FromExpression<HangfireEventPublisher>(p => p.DispatchInScope(new ShutdownEvent(), CancellationToken.None));
        using var services = Services<WaitingHandler, RecordingHandler>();
        var performing = Task.Run(() => Perform(services, job));

        // Act
        await _shutdown.CancelAsync();
        var act = () => performing.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert: Hangfire's worker requeues a job that ends with OperationCanceledException during shutdown.
        await act.Should().ThrowAsync<OperationCanceledException>();
        _handled.Should().BeEmpty();
    }

    private ServiceProvider Services<TFirst, TSecond>()
        where TFirst : class, IEventHandler<ShutdownEvent>
        where TSecond : class, IEventHandler<ShutdownEvent>
    {
        var services = new ServiceCollection().AddLogging().AddSolCQRS();
        services.AddSingleton(_handled).AddSingleton(_shutdown);
        services.AddScoped<IEventHandler<ShutdownEvent>, TFirst>().AddScoped<IEventHandler<ShutdownEvent>, TSecond>();
        return services.BuildServiceProvider();
    }

    private void Perform(IServiceProvider services, Job job)
    {
        var publisher = new HangfireEventPublisher(Substitute.For<IBackgroundJobClient>(),
            services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new PersistentEventsOptions()));
        var cancellation = Substitute.For<IJobCancellationToken>();
        cancellation.ShutdownToken.Returns(_shutdown.Token);
        var context = new PerformContext(Substitute.For<JobStorage>(), Substitute.For<IStorageConnection>(),
            new BackgroundJob("job-1", job, DateTime.UtcNow), cancellation);
        new BackgroundJobPerformer(new JobFilterProviderCollection(), new InstanceActivator(publisher)).Perform(context);
    }

    public sealed record ShutdownEvent : IEvent;

    private sealed class RecordingHandler(ConcurrentQueue<string> handled) : IEventHandler<ShutdownEvent>
    {
        public Task Handle(ShutdownEvent notification, CancellationToken cancellationToken)
        {
            handled.Enqueue("recorded");
            return Task.CompletedTask;
        }
    }

    private sealed class ShutdownAfterWorkHandler(ConcurrentQueue<string> handled, CancellationTokenSource shutdown) : IEventHandler<ShutdownEvent>
    {
        public Task Handle(ShutdownEvent notification, CancellationToken cancellationToken)
        {
            handled.Enqueue("finished");
            return shutdown.CancelAsync();
        }
    }

    private sealed class WaitingHandler : IEventHandler<ShutdownEvent>
    {
        public Task Handle(ShutdownEvent notification, CancellationToken cancellationToken) =>
            Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private sealed class InstanceActivator(object instance) : JobActivator
    {
        public override object ActivateJob(Type jobType) => instance;
    }
}
