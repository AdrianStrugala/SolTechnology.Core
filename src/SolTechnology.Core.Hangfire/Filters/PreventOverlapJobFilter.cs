using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

namespace SolTechnology.Core.Hangfire.Filters;

/// <summary>
/// Prevents overlapping execution: cancels a job if another instance with the same
/// method + arguments is already scheduled or processing. Useful for recurring jobs
/// where a previous run is mid-retry and the next cron trigger fires.
/// Apply as <c>[PreventOverlapJobFilter]</c> on the job method, or register globally
/// via <c>UseSolFilters()</c>.
/// </summary>
public sealed class PreventOverlapJobFilter : JobFilterAttribute, IServerFilter
{
    public void OnPerforming(PerformingContext filterContext)
    {
        if (HasExistingInstance(filterContext))
        {
            filterContext.Canceled = true;
        }
    }

    public void OnPerformed(PerformedContext filterContext)
    {
    }

    private static bool HasExistingInstance(PerformingContext filterContext)
    {
        var currentJob = filterContext.BackgroundJob.Job;
        var currentJobId = filterContext.BackgroundJob.Id;

        var api = filterContext.Storage.GetMonitoringApi();

        var scheduled = api.ScheduledJobs(0, int.MaxValue)
            .Where(x => x.Key != currentJobId && IsMatchingInvocation(x.Value.Job, currentJob));

        var processing = api.ProcessingJobs(0, int.MaxValue)
            .Where(x => x.Key != currentJobId && IsMatchingInvocation(x.Value.Job, currentJob));

        return scheduled.Any() || processing.Any();
    }

    private static bool IsMatchingInvocation(Job? candidate, Job current)
    {
        if (candidate is null || candidate.Method != current.Method)
        {
            return false;
        }

        var candidateData = InvocationData.SerializeJob(candidate);
        var currentData = InvocationData.SerializeJob(current);

        return candidateData.Arguments == currentData.Arguments;
    }
}


