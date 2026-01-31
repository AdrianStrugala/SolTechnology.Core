using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.Jobs;

/// <summary>
/// Error type for Hangfire job failures with job-specific context.
/// </summary>
public class JobError : Error
{
    public string JobName { get; init; } = string.Empty;
    public string? JobId { get; init; }
    public int AttemptNumber { get; init; }

    public static JobError Retryable(string jobName, string message, string? jobId = null, int attemptNumber = 0)
    {
        return new JobError
        {
            JobName = jobName,
            JobId = jobId,
            AttemptNumber = attemptNumber,
            Message = message,
            Recoverable = true
        };
    }

    public static JobError NonRetryable(string jobName, string message, string? jobId = null, int attemptNumber = 0)
    {
        return new JobError
        {
            JobName = jobName,
            JobId = jobId,
            AttemptNumber = attemptNumber,
            Message = message,
            Recoverable = false
        };
    }

    public static JobError FromError(Error error, string jobName, string? jobId = null, int attemptNumber = 0)
    {
        return new JobError
        {
            JobName = jobName,
            JobId = jobId,
            AttemptNumber = attemptNumber,
            Message = error.Message,
            Recoverable = error.Recoverable
        };
    }
}
