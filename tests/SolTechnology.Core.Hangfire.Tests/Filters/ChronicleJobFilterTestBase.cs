using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Hangfire.Filters;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

/// <summary>A real Chronicle whose reports land in a <see cref="FakeLogCollector"/>, plus Hangfire context builders.</summary>
public abstract class ChronicleJobFilterTestBase
{
    protected FakeLogCollector Logs { get; private set; } = null!;
    protected ServiceProvider Services { get; private set; } = null!;
    protected CancellationTokenSource Shutdown { get; private set; } = null!;

    protected IChronicle Chronicle => Services.GetRequiredService<IChronicle>();

    protected IReadOnlyList<FakeLogRecord> Reports => [.. Logs.GetSnapshot().Where(r => r.Id.Id == 2154)];

    internal ChronicleJobFilter Filter => new(Chronicle);

    [SetUp]
    public void SetUpChronicle()
    {
        Logs = new FakeLogCollector();
        Shutdown = new CancellationTokenSource();
        var services = new ServiceCollection()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(new FakeLoggerProvider(Logs)));
        Configure(services);
        services.AddSolChronicle();
        Services = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDownChronicle()
    {
        Services.Dispose();
        Shutdown.Dispose();
    }

    protected virtual void Configure(IServiceCollection services)
    {
    }

    /// <summary>Runs synchronous filter code the way a Hangfire worker does: on a thread with its own ambient state.</summary>
    protected static T OnWorkerThread<T>(Func<T> work)
    {
        using (ExecutionContext.SuppressFlow())
        {
            return Task.Run(work).GetAwaiter().GetResult();
        }
    }

    protected PerformContext PerformContext(Job job, IStorageConnection? connection = null, JobStorage? storage = null, string jobId = "job-1")
    {
        var cancellation = Substitute.For<IJobCancellationToken>();
        cancellation.ShutdownToken.Returns(Shutdown.Token);
        connection ??= Substitute.For<IStorageConnection>();
        if (storage is null)
        {
            connection.GetJobParameter(Arg.Any<string>(), Arg.Any<string>()).Returns((string?)null);
        }

        return new PerformContext(storage ?? Substitute.For<JobStorage>(), connection,
            new BackgroundJob(jobId, job, DateTime.UtcNow), cancellation);
    }

    protected void Perform(Job job, object? result = null, bool canceled = false, Exception? exception = null)
    {
        var activeAfterJob = OnWorkerThread(() =>
        {
            var filter = Filter;
            var performing = new PerformingContext(PerformContext(job));
            filter.OnPerforming(performing);
            filter.OnPerformed(new PerformedContext(performing, result, canceled, exception));
            return Chronicle.IsActive;
        });
        activeAfterJob.Should().BeFalse();
    }

    protected static Job RunnerJob<TJob>() where TJob : class, IJob
    {
        var runner = typeof(RecurringJobRunner<TJob>);
        return new Job(runner, runner.GetMethod(nameof(RecurringJobRunner<TJob>.RunAsync))!, CancellationToken.None);
    }

    protected sealed class InstanceActivator(object instance) : JobActivator
    {
        public override object ActivateJob(Type jobType) => instance;
    }

    protected sealed class ServiceProviderActivator(IServiceProvider services) : JobActivator
    {
        public override object ActivateJob(Type jobType) => services.GetRequiredService(jobType);
    }
}
