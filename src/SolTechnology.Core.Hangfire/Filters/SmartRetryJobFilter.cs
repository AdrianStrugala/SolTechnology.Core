using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;

namespace SolTechnology.Core.Hangfire.Filters;

/// <summary>
/// Inspects the job return value: if the handler returns a failed <see cref="Result"/>
/// with <see cref="Errors.Error.Recoverable"/> = true, forces the job into <see cref="FailedState"/>
/// so Hangfire retries it. Non-recoverable failures are left as succeeded (no pointless retries).
/// This bridges the Result pattern with Hangfire's exception-only retry model.
/// </summary>
internal sealed class SmartRetryJobFilter : JobFilterAttribute, IServerFilter, IElectStateFilter
{
    private const string RetryErrorKey = "SmartRetry_Error";

    public void OnStateElection(ElectStateContext context)
    {
        var errorMessage = context.GetJobParameter<string>(RetryErrorKey);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            context.CandidateState = new FailedState(new InvalidOperationException(errorMessage));
            context.SetJobParameter<string?>(RetryErrorKey, null);
        }
    }

    public void OnPerforming(PerformingContext filterContext)
    {
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        if (filterContext.Canceled || filterContext.Exception != null)
        {
            return;
        }

        if (filterContext.Result is Result result && !result.IsSuccess && result.Error?.Recoverable == true)
        {
            filterContext.SetJobParameter(RetryErrorKey, result.Error.Message);
        }
    }
}


