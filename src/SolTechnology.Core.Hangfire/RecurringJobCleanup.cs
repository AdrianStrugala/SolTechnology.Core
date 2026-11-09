using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Removes recurring entries this application no longer registers: ids recorded in its own tracked set
/// (<c>sol:recurring-ids:&lt;ApplicationName&gt;</c>), so applications that share one storage never delete each
/// other's jobs, and the <c>Type.Name</c> ids that 1.x used.
/// </summary>
internal sealed class RecurringJobCleanup(
    IRecurringJobManager recurringJobManager,
    IStorageConnection connection,
    string applicationName,
    ILogger logger)
{
    private string TrackedSetKey { get; } =
        StorageKey.Fit($"sol:recurring-ids:{applicationName}", $"sol:recurring-ids:{applicationName}", StorageKey.MaxKeyLength);

    public void RemoveStaleIds(IReadOnlyCollection<RecurringJobDescriptor> registered)
    {
        var currentIds = registered.Select(descriptor => descriptor.JobId).ToHashSet();
        var staleIds = connection.GetAllItemsFromSet(TrackedSetKey).Where(id => !currentIds.Contains(id)).ToList();
        foreach (var staleId in staleIds)
        {
            recurringJobManager.RemoveIfExists(staleId);
            logger.LogInformation("Removed recurring job [{JobId}]: no longer registered by [{ApplicationName}]", staleId, applicationName);
        }

        using var transaction = connection.CreateWriteTransaction();
        foreach (var staleId in staleIds)
        {
            transaction.RemoveFromSet(TrackedSetKey, staleId);
        }

        foreach (var currentId in currentIds)
        {
            transaction.AddToSet(TrackedSetKey, currentId);
        }

        transaction.Commit();
    }

    // Only an entry that runs this job's runner is removed, so another application's same-named job survives.
    public void RemoveLegacyIds(IReadOnlyCollection<RecurringJobDescriptor> registered)
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
