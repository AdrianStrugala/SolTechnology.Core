using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Removes recurring entries this application no longer registers: always the <c>Type.Name</c> ids that 1.x used,
/// and, when <see cref="RecurringJobsOptions.RemoveStaleRecurringJobs"/> is on, the ids recorded in its own tracked
/// set (<c>sol:recurring-ids:&lt;ApplicationName&gt;</c>) that it no longer registers.
/// </summary>
internal sealed class RecurringJobCleanup(
    IRecurringJobManager recurringJobManager,
    JobStorage jobStorage,
    IHostEnvironment hostEnvironment,
    IOptions<RecurringJobsOptions> options,
    ILogger<RecurringJobCleanup> logger)
{
    public void Run(IReadOnlyCollection<RecurringJobDescriptor> registered)
    {
        using var connection = jobStorage.GetConnection();
        RemoveLegacyIds(connection, registered);
        if (options.Value.RemoveStaleRecurringJobs)
        {
            RemoveStaleIds(connection, registered);
        }
    }

    private void RemoveStaleIds(IStorageConnection connection, IReadOnlyCollection<RecurringJobDescriptor> registered)
    {
        var applicationName = hostEnvironment.ApplicationName;
        var trackedSetKey = StorageKey.Fit($"sol:recurring-ids:{applicationName}", $"sol:recurring-ids:{applicationName}", StorageKey.MaxKeyLength);
        var currentIds = registered.Select(descriptor => descriptor.JobId).ToHashSet();
        var staleIds = connection.GetAllItemsFromSet(trackedSetKey).Where(id => !currentIds.Contains(id)).ToList();
        foreach (var staleId in staleIds)
        {
            recurringJobManager.RemoveIfExists(staleId);
            logger.LogInformation("Removed recurring job [{JobId}]: no longer registered by [{ApplicationName}]", staleId, applicationName);
        }

        using var transaction = connection.CreateWriteTransaction();
        foreach (var staleId in staleIds)
        {
            transaction.RemoveFromSet(trackedSetKey, staleId);
        }

        foreach (var currentId in currentIds)
        {
            transaction.AddToSet(trackedSetKey, currentId);
        }

        transaction.Commit();
    }

    // Only an entry that runs this job's runner is removed, so another application's same-named job survives.
    private void RemoveLegacyIds(IStorageConnection connection, IReadOnlyCollection<RecurringJobDescriptor> registered)
    {
        var currentIds = registered.Select(descriptor => descriptor.JobId).ToHashSet();
        foreach (var descriptor in registered.Where(descriptor => !currentIds.Contains(descriptor.LegacyJobId)))
        {
            var legacy = connection.GetRecurringJobs([descriptor.LegacyJobId]).SingleOrDefault();
            if (legacy is { Removed: false } && legacy.Job?.Type == descriptor.RunnerType)
            {
                recurringJobManager.RemoveIfExists(descriptor.LegacyJobId);
                logger.LogInformation("Removed 1.x recurring job [{LegacyJobId}]; it runs as [{JobId}]", descriptor.LegacyJobId, descriptor.JobId);
            }
        }
    }
}
