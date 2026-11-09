using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Source-generated start-up logs of recurring job registration (<c>EventId</c> 2150–2153, after CQRS's 2149).
/// </summary>
internal static partial class RecurringJobLogMessages
{
    [LoggerMessage(
        EventId = 2150,
        EventName = nameof(OverlapGuardNotInstalled),
        Level = LogLevel.Warning,
        Message = "Recurring job [{JobId}] requested preventOverlap but UseSolFilters was not called; overlap is not prevented.")]
    public static partial void OverlapGuardNotInstalled(ILogger logger, string jobId);

    [LoggerMessage(
        EventId = 2151,
        EventName = nameof(SmartRetryNotInstalled),
        Level = LogLevel.Warning,
        Message = "[{JobCount}] recurring jobs are registered but UseSolFilters was not called; a recoverable failed Result is not retried.")]
    public static partial void SmartRetryNotInstalled(ILogger logger, int jobCount);

    [LoggerMessage(
        EventId = 2152,
        EventName = nameof(StaleJobRemoved),
        Level = LogLevel.Information,
        Message = "Removed recurring job [{JobId}]: no longer registered by [{ApplicationName}]")]
    public static partial void StaleJobRemoved(ILogger logger, string jobId, string applicationName);

    [LoggerMessage(
        EventId = 2153,
        EventName = nameof(LegacyJobRemoved),
        Level = LogLevel.Information,
        Message = "Removed 1.x recurring job [{LegacyJobId}]; it runs as [{JobId}]")]
    public static partial void LegacyJobRemoved(ILogger logger, string legacyJobId, string jobId);
}
