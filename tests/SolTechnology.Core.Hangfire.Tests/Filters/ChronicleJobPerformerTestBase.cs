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

/// <summary>Runs <see cref="ChronicleJobFilter"/> through Hangfire's real <see cref="BackgroundJobPerformer"/>.</summary>
public abstract class ChronicleJobPerformerTestBase : ChronicleJobFilterTestBase
{
    protected InMemoryStorage Storage { get; private set; } = null!;

    [SetUp]
    public void SetUpStorage() => Storage = new InMemoryStorage();

    [TearDown]
    public void TearDownStorage() => Storage.Dispose();

    protected override void Configure(IServiceCollection services)
    {
        services.AddSolCorrelationIdService();
        services.AddSolCQRS();
        services.AddSingleton(new ConcurrentQueue<string>());
        services.AddScoped<IEventHandler<ShutdownEvent>, WaitingHandler>().AddScoped<IEventHandler<ShutdownEvent>, RecordingHandler>();
        services.AddScoped<NonRecoverableFailingJob>().AddSingleton<RecurringJobRunner<NonRecoverableFailingJob>>();
        services.AddScoped<TestJob>().AddSingleton<RecurringJobRunner<TestJob>>();
    }

    protected bool PerformProbe(JobFilterCollection filters, string step)
        => OnWorkerThread(() => PerformOnThisThread(filters, ProbeJob(step)));

    protected Job ProbeJob(string step) => Job.FromExpression<ChronicleProbeJob>(j => j.Run(step));

    protected bool PerformOnThisThread(JobFilterCollection filters, Job job, JobActivator? activator = null)
    {
        try
        {
            new BackgroundJobPerformer(new JobFilterProviderCollection(filters), activator ?? new InstanceActivator(new ChronicleProbeJob(Chronicle)))
                .Perform(PerformContext(job));
        }
        catch (Exception exception) when (exception is OperationCanceledException or JobPerformanceException or InvalidOperationException)
        {
        }

        return Chronicle.IsActive;
    }

    protected string RunAndChangeState(JobFilterProviderCollection filters, string jobId)
    {
        using var connection = Storage.GetConnection();
        var context = PerformContext(connection.GetJobData(jobId).Job, connection, Storage, jobId);
        var result = new BackgroundJobPerformer(filters, new ServiceProviderActivator(Services)).Perform(context);
        return new BackgroundJobStateChanger(filters)
            .ChangeState(new StateChangeContext(Storage, connection, jobId, new SucceededState(result, 0, 0))).Name;
    }
}
