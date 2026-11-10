using System.Collections.Concurrent;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Hangfire.Filters;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

[TestFixture]
public class ChronicleJobFilterPerformerOutcomeTests : ChronicleJobPerformerTestBase
{
    [Test]
    public void RecurringJob_FailedResult_ThroughRealPerformer_RootFailed()
    {
        // Arrange
        var filters = new JobFilterProviderCollection(new JobFilterCollection { Filter });
        var jobId = new BackgroundJobClient(Storage, filters).Create(RunnerJob<NonRecoverableFailingJob>(), new EnqueuedState());

        // Act
        var state = OnWorkerThread(() => RunAndChangeState(filters, jobId));

        // Assert
        state.Should().Be(SucceededState.StateName);
        var report = Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Error);
        report.Message.Should().StartWith("Chronicle [Job NonRecoverableFailingJob] [Failed]").And.Contain("Recoverable=False");
    }

    [Test]
    public void OverlapGuardCancels_ThroughRealPerformer_StartsNoRootAndEmitsNothing()
    {
        // Arrange
        var overlap = new PreventOverlapFilterProvider([new RecurringJobDescriptor(typeof(TestJob), "* * * * *", preventOverlap: true)]);
        var filters = new JobFilterProviderCollection(new JobFilterCollection { Filter }, overlap);
        var jobId = new BackgroundJobClient(Storage, filters).Create(RunnerJob<TestJob>(), new EnqueuedState());
        using var otherServer = Storage.GetConnection();
        otherServer.SetJobParameter(jobId, "RecurringJobId", "\"nightly\"");
        using var heldLock = otherServer.AcquireDistributedLock("sol:overlap:nightly", TimeSpan.Zero);

        // Act
        var activeAfterRun = OnWorkerThread(() =>
        {
            using var connection = Storage.GetConnection();
            new BackgroundJobPerformer(filters, new ServiceProviderActivator(Services))
                .Perform(PerformContext(connection.GetJobData(jobId).Job, connection, Storage, jobId));
            return Chronicle.IsActive;
        });

        // Assert
        activeAfterRun.Should().BeFalse();
        Reports.Should().BeEmpty();
    }

    [Test]
    public async Task EventDispatch_ShutdownCancellation_ThroughRealPerformer_RootCancelledAndHandlerStepCancelled()
    {
        // Arrange
        var publisher = new HangfireEventPublisher(Substitute.For<IBackgroundJobClient>(),
            Services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new PersistentEventsOptions()));
        var job = Job.FromExpression<HangfireEventPublisher>(p => p.DispatchInScope(new ShutdownEvent(), CancellationToken.None));
        var performing = Task.Run(() => OnWorkerThread(() =>
            PerformOnThisThread(new JobFilterCollection { Filter }, job, new InstanceActivator(publisher))));

        // Act
        await Shutdown.CancelAsync();
        var activeAfterJob = await performing.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        activeAfterJob.Should().BeFalse();
        Services.GetRequiredService<ConcurrentQueue<string>>().Should().BeEmpty();
        var report = Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Warning);
        report.Message.Should().StartWith("Chronicle [Event ShutdownEvent] [Cancelled]")
            .And.Contain("`- Event ShutdownEvent WaitingHandler [@").And.NotContain("RecordingHandler");
    }
}
