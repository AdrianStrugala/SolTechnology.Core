using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire.Tests;

/// <summary>
/// Runs recurring jobs through Hangfire's performer and state machine, as a worker would, with Smart Retry and
/// Hangfire's default global retry filter installed (premortem RM-9).
/// </summary>
[TestFixture]
public class RecurringJobRetryTests
{
    private InMemoryStorage _storage = null!;
    private JobFilterProviderCollection _filters = null!;
    private ServiceProvider _services = null!;

    [SetUp]
    public void Setup()
    {
        _storage = new InMemoryStorage();
        var globalFilters = new JobFilterCollection();
        globalFilters.Add(new AutomaticRetryAttribute());
        globalFilters.Add(new SmartRetryJobFilter());
        _filters = new JobFilterProviderCollection(globalFilters, new JobFilterAttributeFilterProvider());
        _services = new ServiceCollection()
            .AddScoped<RecoverableFailingJob>().AddSingleton<RecurringJobRunner<RecoverableFailingJob>>()
            .AddScoped<ThrowingJob>().AddSingleton<RecurringJobRunner<ThrowingJob>>()
            .BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _services.Dispose();
        _storage.Dispose();
    }

    [Test]
    public void RecurringJob_RecoverableResult_IsRetriedBySmartRetry()
    {
        // Arrange
        var jobId = CreateRunnerJob<RecoverableFailingJob>();

        // Act
        var state = RunOnce(jobId);

        // Assert
        state.Should().Be(ScheduledState.StateName);
    }

    [Test]
    public void RecurringJob_RecoverableResult_RetriedAtMostConfiguredTimes()
    {
        // Arrange
        var jobId = CreateRunnerJob<RecoverableFailingJob>();

        // Act
        var states = Enumerable.Range(0, RecurringJobRetry.RecoverableResultAttempts + 1).Select(_ => RunOnce(jobId)).ToList();

        // Assert
        states.Should().Equal(
            Enumerable.Repeat(ScheduledState.StateName, RecurringJobRetry.RecoverableResultAttempts).Append(FailedState.StateName));
    }

    [Test]
    public void RecurringJob_Throws_IsNotRetried()
    {
        // Arrange
        var jobId = CreateRunnerJob<ThrowingJob>();

        // Act
        var state = RunOnce(jobId);

        // Assert
        state.Should().Be(FailedState.StateName);
    }

    private string CreateRunnerJob<TJob>() where TJob : class, IJob
    {
        var runner = typeof(RecurringJobRunner<TJob>);
        var job = new Job(runner, runner.GetMethod("RunAsync")!, CancellationToken.None);
        return new BackgroundJobClient(_storage, _filters).Create(job, new EnqueuedState());
    }

    private string RunOnce(string jobId)
    {
        using var connection = _storage.GetConnection();
        var job = connection.GetJobData(jobId).Job;
        var context = new PerformContext(_storage, connection, new BackgroundJob(jobId, job, DateTime.UtcNow), new JobCancellationToken(false));
        IState outcome;
        try
        {
            var result = new BackgroundJobPerformer(_filters, new ServiceProviderActivator(_services)).Perform(context);
            outcome = new SucceededState(result, 0, 0);
        }
        catch (JobPerformanceException exception)
        {
            outcome = new FailedState(exception.InnerException);
        }

        return new BackgroundJobStateChanger(_filters)
            .ChangeState(new StateChangeContext(_storage, connection, jobId, outcome)).Name;
    }

    private sealed class ServiceProviderActivator(IServiceProvider services) : JobActivator
    {
        public override object ActivateJob(Type jobType) => services.GetRequiredService(jobType);
    }
}
