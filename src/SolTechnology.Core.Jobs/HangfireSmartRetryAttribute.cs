using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Jobs;

/// <summary>
/// Hangfire filter that implements smart retry logic with configurable delays.
/// Supports error-based retry decisions (recoverable vs non-recoverable errors).
/// </summary>
public class HangfireSmartRetryAttribute : JobFilterAttribute, IElectStateFilter, IApplyStateFilter
{
    private static readonly int[] DefaultDelaysInSeconds = [10, 60, 300, 1800, 3600, 7200, 14400, 28800, 57600, 86400];

    private readonly int[] _delaysInSeconds;
    private readonly ILogger<HangfireSmartRetryAttribute>? _logger;

    public int MaxAttempts => _delaysInSeconds.Length + 1;

    public HangfireSmartRetryAttribute() : this(DefaultDelaysInSeconds, null)
    {
    }

    public HangfireSmartRetryAttribute(int[] delaysInSeconds, ILogger<HangfireSmartRetryAttribute>? logger = null)
    {
        _delaysInSeconds = delaysInSeconds;
        _logger = logger;
    }

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not FailedState failedState)
        {
            return;
        }

        var retryAttempt = context.GetJobParameter<int>("RetryCount") + 1;
        var jobError = ExtractJobError(failedState.Exception);

        _logger?.LogWarning(
            "Job [{JobId}] failed (attempt [{Attempt}]/[{MaxAttempts}]). Recoverable: [{Recoverable}]. Error: [{Error}]",
            context.BackgroundJob.Id,
            retryAttempt,
            MaxAttempts,
            jobError?.Recoverable ?? true,
            jobError?.Message ?? failedState.Exception?.Message);

        if (jobError != null && !jobError.Recoverable)
        {
            _logger?.LogError(
                "Job [{JobId}] marked as non-recoverable. Moving to failed state permanently. Error: [{Error}]",
                context.BackgroundJob.Id,
                jobError.Message);
            return;
        }

        if (retryAttempt <= _delaysInSeconds.Length)
        {
            var delay = TimeSpan.FromSeconds(_delaysInSeconds[retryAttempt - 1]);

            _logger?.LogInformation(
                "Scheduling retry for job [{JobId}] in [{Delay}]",
                context.BackgroundJob.Id,
                delay);

            context.SetJobParameter("RetryCount", retryAttempt);
            context.CandidateState = new ScheduledState(delay)
            {
                Reason = $"Retry attempt {retryAttempt} of {MaxAttempts}. Scheduled after {delay.TotalSeconds}s delay."
            };
        }
        else
        {
            _logger?.LogError(
                "Job [{JobId}] exceeded maximum retry attempts ([{MaxAttempts}]). Moving to failed state.",
                context.BackgroundJob.Id,
                MaxAttempts);
        }
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is ScheduledState scheduledState)
        {
            var retryAttempt = context.GetJobParameter<int>("RetryCount");

            _logger?.LogDebug(
                "Job [{JobId}] retry [{Attempt}] scheduled for [{EnqueueAt}]",
                context.BackgroundJob.Id,
                retryAttempt,
                scheduledState.EnqueueAt);
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
    }

    private static JobError? ExtractJobError(Exception? exception)
    {
        if (exception == null)
        {
            return null;
        }

        if (exception is JobExecutionException jobEx)
        {
            return jobEx.JobError;
        }

        if (exception.InnerException != null)
        {
            return ExtractJobError(exception.InnerException);
        }

        return null;
    }
}

/// <summary>
/// Exception that carries job error information for smart retry decisions.
/// </summary>
public class JobExecutionException : Exception
{
    public JobError JobError { get; }

    public JobExecutionException(JobError jobError)
        : base(jobError.Message)
    {
        JobError = jobError;
    }

    public JobExecutionException(JobError jobError, Exception innerException)
        : base(jobError.Message, innerException)
    {
        JobError = jobError;
    }
}
