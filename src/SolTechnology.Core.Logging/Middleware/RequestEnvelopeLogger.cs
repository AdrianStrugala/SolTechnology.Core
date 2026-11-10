using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Middleware;

/// <summary>
/// Source-generated log entries written by <see cref="LoggingMiddleware"/> and
/// <see cref="RequestBodyBuffer"/>: the request envelope, its failure and abort entries, and the
/// scope-building warnings. Event ids continue the 2137–2140 operation range; 2147 is retired.
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
    /// Writes the request's single outcome entry: the 2155 Chronicle report when the request root
    /// recorded, otherwise 2144 (client abort), 2145 (unhandled exception), or 2146 (finished).
    /// </summary>
    public static void RequestEnded(
        ILogger logger, HttpContext context, ChronicleStep root, Exception? exception, bool clientAborted, long elapsedMs)
    {
        var request = context.Request;
        if (ChronicleRequestRoot.Complete(root, context, exception, clientAborted) is { } reported)
        {
            ChronicleRequestRoot.Report(logger, reported, context, exception, elapsedMs);
        }
        else if (exception is not null && clientAborted)
        {
            RequestAborted(logger, exception, request.Method, request.Path, elapsedMs);
        }
        else if (exception is not null)
        {
            RequestFailed(logger, exception, request.Method, request.Path, elapsedMs);
        }
        else
        {
            RequestFinished(logger, context, elapsedMs);
        }
    }

    /// <summary>
    /// Writes the Finished entry at a status-code-aware level (Information below 400, Warning 4xx, Error 5xx).
    /// </summary>
    public static void RequestFinished(ILogger logger, HttpContext context, long elapsedMs)
    {
        var request = context.Request;
        var statusCode = context.Response.StatusCode;
        var level = statusCode >= 500 ? LogLevel.Error
                  : statusCode >= 400 ? LogLevel.Warning
                                      : LogLevel.Information;
        RequestFinished(logger, level, request.Method, request.Path, statusCode, elapsedMs);
    }

    [LoggerMessage(EventId = 2146,
        Message = "Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms]")]
    private static partial void RequestFinished(
        ILogger logger, LogLevel level, string requestMethod, PathString requestPath, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 2148, Level = LogLevel.Debug,
        Message = "Skipping body-based log enrichment for [{RequestPath}] - JSON could not be parsed")]
    public static partial void BodyEnrichmentSkipped(ILogger logger, Exception exception, PathString requestPath);
}
