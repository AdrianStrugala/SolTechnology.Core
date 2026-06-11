using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Hosting;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Registers all recurring jobs with Hangfire on host startup.
/// Deferred to <see cref="IHostedService.StartAsync"/> so that <see cref="JobStorage"/> is configured.
/// </summary>
internal sealed class RecurringJobRegistrar(
    IRecurringJobManager recurringJobManager,
    IEnumerable<RecurringJobDescriptor> descriptors) : IHostedService
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
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}


