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
using SolTechnology.Core.Testing.Substitutes;

namespace SolTechnology.Core.Hangfire.Tests;

/// <summary>Performs event jobs through Hangfire's own performer, as a server would.</summary>
[TestFixture]
public class HangfireEventDispatchTests
{
    // Written by 1.x Enqueue(publisher => publisher.DispatchInScope(@event)) with UseSimpleAssemblyNameTypeSerializer.
    private const string Legacy1xPayload =
        """{"Type":"SolTechnology.Core.Hangfire.HangfireEventPublisher, SolTechnology.Core.Hangfire","Method":"DispatchInScope","ParameterTypes":"[\"SolTechnology.Core.CQRS.IEvent, SolTechnology.Core.CQRS\"]","Arguments":"[\"{\\\"$type\\\":\\\"SolTechnology.Core.Hangfire.Tests.TestEvent, SolTechnology.Core.Hangfire.Tests\\\",\\\"Message\\\":\\\"legacy\\\"}\"]"}""";

    private IEventDispatcher _dispatcher = null!;
    private HangfireEventPublisher _sut = null!;

    [OneTimeSetUp]
    public void UseConsumerSerializerSettings() => GlobalConfiguration.Configuration.UseRecommendedSerializerSettings();

    [OneTimeTearDown]
    public void ResetSerializerSettings() => GlobalConfiguration.Configuration.UseSerializerSettings(null);

    [SetUp]
    public void Setup()
    {
        _dispatcher = Substitute.For<IEventDispatcher>();
        var services = new ServiceCollection().AddScoped(_ => _dispatcher).BuildServiceProvider();
        _sut = new HangfireEventPublisher(
            Substitute.For<IBackgroundJobClient>(),
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new PersistentEventsOptions()));
    }

    [Test]
    public async Task DispatchInScope_ShutdownToken_CancelsHandler()
    {
        // Arrange
        using var shutdown = new CancellationTokenSource();
        _dispatcher.Dispatch(Arg.Any<IEvent>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.Delay(Timeout.Infinite, call.ArgAt<CancellationToken>(1)));
        var job = Job.FromExpression<HangfireEventPublisher>(p => p.DispatchInScope(new TestEvent("x"), CancellationToken.None));
        var performing = Task.Run(() => Perform(job, shutdown.Token));

        // Act
        await shutdown.CancelAsync();
        var act = () => performing.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        (await act.Should().ThrowAsync<Exception>()).Which
            .Should().Match<Exception>(e => e is OperationCanceledException || e.InnerException is OperationCanceledException);
    }

    [Test]
    public async Task DispatchInScope_ShutdownSwallowedByDispatcher_AbortsJob()
    {
        // Arrange
        using var shutdown = new CancellationTokenSource();
        var services = new ServiceCollection().AddLogging()
            .AddSolCQRS(o => o.RegisterEventsFromAssembly(typeof(HangfireEventDispatchTests).Assembly))
            .BuildServiceProvider();
        _sut = new HangfireEventPublisher(Substitute.For<IBackgroundJobClient>(),
            services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new PersistentEventsOptions()));
        var job = Job.FromExpression<HangfireEventPublisher>(p => p.DispatchInScope(new SlowEvent(), CancellationToken.None));
        var performing = Task.Run(() => Perform(job, shutdown.Token));

        // Act
        await shutdown.CancelAsync();
        var act = () => performing.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void DispatchInScope_Legacy1xJob_StillRuns()
    {
        // Arrange
        _dispatcher.Dispatch(Arg.Any<IEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var job = InvocationData.DeserializePayload(Legacy1xPayload).DeserializeJob();

        // Act
        Perform(job, CancellationToken.None);

        // Assert
        job.Method.GetParameters().Should().ContainSingle();
        _dispatcher.Received(1).Dispatch(new TestEvent("legacy"), Ct.Any);
    }

    private object? Perform(Job job, CancellationToken shutdownToken)
    {
        var cancellation = Substitute.For<IJobCancellationToken>();
        cancellation.ShutdownToken.Returns(shutdownToken);
        var context = new PerformContext(Substitute.For<JobStorage>(), Substitute.For<IStorageConnection>(),
            new BackgroundJob("job-1", job, DateTime.UtcNow), cancellation);
        return new BackgroundJobPerformer(new JobFilterProviderCollection(), new SingleInstanceActivator(_sut)).Perform(context);
    }

    public sealed record SlowEvent : IEvent;

    public sealed class SlowEventHandler : IEventHandler<SlowEvent>
    {
        public Task Handle(SlowEvent notification, CancellationToken cancellationToken) =>
            Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private sealed class SingleInstanceActivator(object instance) : JobActivator
    {
        public override object ActivateJob(Type jobType) => instance;
    }
}
