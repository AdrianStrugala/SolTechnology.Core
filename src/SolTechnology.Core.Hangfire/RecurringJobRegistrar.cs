using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Registers all recurring jobs with Hangfire on host startup, then removes the entries this application
/// no longer registers (see <see cref="RecurringJobCleanup"/>).
/// Deferred to <see cref="IHostedService.StartAsync"/> so that <see cref="JobStorage"/> is configured.
/// </summary>
internal sealed class RecurringJobRegistrar(
    IRecurringJobManager recurringJobManager,
    IEnumerable<RecurringJobDescriptor> descriptors,
    PreventOverlapFilterProvider overlapFilterProvider,
    RecurringJobCleanup cleanup,
    ILogger<RecurringJobRegistrar> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var registered = descriptors.ToList();
        foreach (var descriptor in registered)
        {
            Register(descriptor);
        }

        if (registered.Count > 0 && !GlobalJobFilters.Filters.Any(filter => filter.Instance is SmartRetryJobFilter))
        {
            RecurringJobLogMessages.SmartRetryNotInstalled(logger, registered.Count);
        }

        cleanup.Run(registered);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void Register(RecurringJobDescriptor descriptor)
    {
        var methodInfo = descriptor.RunnerType.GetMethod(nameof(RecurringJobRunner<IJob>.RunAsync))!;
        recurringJobManager.AddOrUpdate(
            descriptor.JobId,
            new Job(descriptor.RunnerType, methodInfo, CancellationToken.None),
            descriptor.CronExpression,
            new RecurringJobOptions { TimeZone = descriptor.TimeZone });

        if (descriptor.PreventOverlap && !overlapFilterProvider.IsInstalled)
        {
            RecurringJobLogMessages.OverlapGuardNotInstalled(logger, descriptor.JobId);
        }
    }
}
