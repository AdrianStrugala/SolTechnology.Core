using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Registers all recurring jobs with Hangfire on host startup.
/// Deferred to <see cref="IHostedService.StartAsync"/> so that <see cref="JobStorage"/> is configured.
/// </summary>
internal sealed class RecurringJobRegistrar(
    IRecurringJobManager recurringJobManager,
    IEnumerable<RecurringJobDescriptor> descriptors,
    PreventOverlapFilterProvider overlapFilterProvider,
    ILogger<RecurringJobRegistrar> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var descriptor in descriptors)
        {
            var runnerType = typeof(RecurringJobRunner<>).MakeGenericType(descriptor.JobType);
            var methodInfo = runnerType.GetMethod(nameof(RecurringJobRunner<IJob>.RunAsync))!;
            var jobId = descriptor.JobType.Name;

            recurringJobManager.AddOrUpdate(
                jobId,
                new Job(runnerType, methodInfo, CancellationToken.None),
                descriptor.CronExpression);

            if (descriptor.PreventOverlap && !overlapFilterProvider.IsInstalled)
            {
                logger.LogWarning(
                    "Recurring job [{JobId}] requested preventOverlap but UseSolFilters was not called; overlap is not prevented.",
                    jobId);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
