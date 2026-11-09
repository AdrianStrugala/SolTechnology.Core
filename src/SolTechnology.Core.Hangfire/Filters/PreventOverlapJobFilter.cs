using System.Security.Cryptography;
using System.Text;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

namespace SolTechnology.Core.Hangfire.Filters;

/// <summary>
/// Prevents overlapping execution: a run is cancelled while another run of the same recurring job
/// (or, outside recurring jobs, of the same method with the same arguments) is processing.
/// Holds a non-blocking distributed storage lock from <see cref="OnPerforming"/> to <see cref="OnPerformed"/>,
/// so the check costs the same whatever the size of the storage.
/// Apply as <c>[PreventOverlapJobFilter]</c> on a job method, or pass <c>preventOverlap: true</c> to
/// <c>AddSolRecurringJob</c> and call <c>UseSolFilters()</c>.
/// </summary>
public sealed class PreventOverlapJobFilter : JobFilterAttribute, IServerFilter
{
    private const string LockItemKey = "SolTechnology.PreventOverlapLock";
    private const string RecurringJobIdParameter = "RecurringJobId";

    /// <summary>Takes the overlap lock, or cancels the run when another matching run holds it.</summary>
    public void OnPerforming(PerformingContext filterContext)
    {
        try
        {
            filterContext.Items[LockItemKey] = filterContext.Connection.AcquireDistributedLock(
                LockResource(filterContext), TimeSpan.Zero);
        }
        catch (DistributedLockTimeoutException)
        {
            filterContext.Canceled = true;
        }
    }

    /// <summary>Releases the overlap lock taken in <see cref="OnPerforming"/>.</summary>
    public void OnPerformed(PerformedContext filterContext)
    {
        if (filterContext.Items.TryGetValue(LockItemKey, out var held) && held is IDisposable overlapLock)
        {
            filterContext.Items.Remove(LockItemKey);
            overlapLock.Dispose();
        }
    }

    private static string LockResource(PerformContext context)
    {
        var recurringJobId = context.GetJobParameter<string>(RecurringJobIdParameter);
        return string.IsNullOrEmpty(recurringJobId)
            ? $"sol:overlap:{InvocationHash(context.BackgroundJob.Job)}"
            : $"sol:overlap:{recurringJobId}";
    }

    // Hashed so the resource name stays short whatever the size of the job arguments.
    private static string InvocationHash(Job job)
    {
        var invocation = InvocationData.SerializeJob(job);
        var identity = $"{invocation.Type}|{invocation.Method}|{invocation.ParameterTypes}|{invocation.Arguments}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }
}
