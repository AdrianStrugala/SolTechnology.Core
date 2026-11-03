using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging.Middleware;

/// <summary>
/// Source-generated log entries written by <see cref="LoggingMiddleware"/> and
/// <see cref="RequestBodyBuffer"/>: the request envelope, its failure and abort entries, and the
/// scope-building warnings. Event ids continue the 2137–2140 operation range.
/// </summary>
internal static partial class RequestEnvelopeLogger
{
    [LoggerMessage(EventId = 2141, Level = LogLevel.Warning,
        Message = "Log scope enricher [{EnricherType}] threw and was skipped")]
    public static partial void EnricherFailed(ILogger logger, Exception exception, string? enricherType);

    [LoggerMessage(EventId = 2142, Level = LogLevel.Warning,
        Message = "Invalid [{Header}] header rejected: [{CorrelationError}]")]
    public static partial void CorrelationHeaderRejected(ILogger logger, string header, string correlationError);

    [LoggerMessage(EventId = 2143, Level = LogLevel.Information,
        Message = "Started request [{RequestMethod}] [{RequestPath}]")]
    public static partial void RequestStarted(ILogger logger, string requestMethod, PathString requestPath);

    [LoggerMessage(EventId = 2144, Level = LogLevel.Warning,
        Message = "Request [{RequestMethod}] [{RequestPath}] aborted by client after [{ElapsedMs} ms]")]
    public static partial void RequestAborted(
        ILogger logger, Exception exception, string requestMethod, PathString requestPath, long elapsedMs);

    [LoggerMessage(EventId = 2145, Level = LogLevel.Error,
        Message = "Request [{RequestMethod}] [{RequestPath}] failed after [{ElapsedMs} ms]")]
    public static partial void RequestFailed(
        ILogger logger, Exception exception, string requestMethod, PathString requestPath, long elapsedMs);

    /// <summary>
    /// Writes the Finished entry at a status-code-aware level (Information below 400, Warning 4xx,
    /// Error 5xx), with the per-request timings when any were recorded.
    /// </summary>
    public static void RequestFinished(ILogger logger, HttpContext context, ITimingService timingService, long elapsedMs)
    {
        var request = context.Request;
        var statusCode = context.Response.StatusCode;
        var level = statusCode >= 500 ? LogLevel.Error
                  : statusCode >= 400 ? LogLevel.Warning
                                      : LogLevel.Information;

        var timings = timingService.GetTimings();
        if (timings.Count > 0)
        {
            RequestFinishedWithTimings(logger, level, request.Method, request.Path, statusCode, elapsedMs, timings);
            return;
        }

        RequestFinished(logger, level, request.Method, request.Path, statusCode, elapsedMs);
    }

    [LoggerMessage(EventId = 2146,
        Message = "Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms]")]
    private static partial void RequestFinished(
        ILogger logger, LogLevel level, string requestMethod, PathString requestPath, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 2147,
        Message = "Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms] — timings: [{@Timings}]")]
    private static partial void RequestFinishedWithTimings(
        ILogger logger, LogLevel level, string requestMethod, PathString requestPath, int statusCode, long elapsedMs,
        IDictionary<string, long> timings);

    [LoggerMessage(EventId = 2148, Level = LogLevel.Debug,
        Message = "Skipping body-based log enrichment for [{RequestPath}] - JSON could not be parsed")]
    public static partial void BodyEnrichmentSkipped(ILogger logger, Exception exception, PathString requestPath);
}
