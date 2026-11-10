using Hangfire.Common;
using Hangfire.Server;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Hangfire.Filters;

/// <summary>
/// Opens a Chronicle root per performed job (<c>Job {TJob}</c> for recurring jobs, <c>Event {EventType}</c> for
/// persistent events) and closes it with the job's outcome: a failed <see cref="Result"/> is <c>Failed</c> even
/// though Hangfire records <c>Succeeded</c>; shutdown and abort are <c>Cancelled</c>. Its <see cref="Order"/> of 0
/// runs it after every default-order filter, so a run the overlap guard cancels opens no root.
/// </summary>
internal sealed class ChronicleJobFilter(IChronicle chronicle) : IServerFilter, IJobFilter
{
    private const string RootKey = "ChronicleRoot";

    public bool AllowMultiple => false;

    public int Order => 0;

    public void OnPerforming(PerformingContext filterContext)
    {
        // Read storage before opening the root: Hangfire skips OnPerformed for a filter whose OnPerforming threw.
        var retryCount = filterContext.GetJobParameter<int>("RetryCount");
        var root = chronicle.BeginRoot(ChronicleJobName.Of(filterContext.BackgroundJob.Job));
        filterContext.Items[RootKey] = root;
        if (root.IsRecording)
        {
            root.Set("JobId", filterContext.BackgroundJob.Id).Set("RetryCount", retryCount);
        }
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        if (!filterContext.Items.Remove(RootKey, out var stored) || stored is not ChronicleStep root)
        {
            return;
        }

        try
        {
            RecordOutcome(root, filterContext);
        }
        finally
        {
            // Always close: a worker thread must never keep a job root as its ambient node (CRM-2).
            root.Dispose();
        }
    }

    private static void RecordOutcome(ChronicleStep root, PerformedContext context)
    {
        switch (context)
        {
            case { Exception: JobAbortedException }:
            case { Exception: OperationCanceledException } when context.CancellationToken.ShutdownToken.IsCancellationRequested:
                root.Cancel();
                break;
            case { Exception: JobPerformanceException { InnerException: { } inner } }:
                root.Fail(inner);
                break;
            case { Exception: { } exception }:
                root.Fail(exception);
                break;
            case { Canceled: true }:
                root.Set("Outcome", "CanceledByFilter").Cancel();
                break;
            case { Result: Result { IsFailure: true } failed }:
                root.Set("Recoverable", failed.Error?.Recoverable ?? false)
                    .Fail(failed.Error?.GetType().Name ?? nameof(Error), failed.Error?.Message);
                break;
        }
    }
}
