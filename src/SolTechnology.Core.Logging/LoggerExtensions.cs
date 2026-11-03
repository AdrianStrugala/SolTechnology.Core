using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging;

/// <summary>
/// Extension methods that emit the canonical operation-lifecycle log events
/// (<c>EventId</c> 2137 START / 2138 SUCCESS / 2139 FAIL / 2140 user message).
/// Backed by a <c>[LoggerMessage]</c> source generator (<c>OperationLogMessages</c>),
/// so every call is allocation-free and short-circuits when the level is disabled.
/// </summary>
// ReSharper disable once InconsistentNaming
public static class ILoggerExtensions
{
    /// <summary>
    /// Emits <c>EventId 2137 OperationStarted</c> with template
    /// <c>"Operation: [{OperationName}]. Status: [START]"</c>. When <paramref name="message"/>
    /// is supplied it is forwarded as a separate <c>EventId 2140</c> entry to keep the START
    /// event template stable for downstream queries.
    /// </summary>
    public static void OperationStarted(this ILogger logger, string operationName, string? message = null)
    {
        if (message is not null)
        {
            // 'message' is user input - never use it as a message template (curly braces break the formatter).
            OperationLogMessages.UserMessage(logger, message);
        }

        OperationLogMessages.OperationStarted(logger, operationName);
    }

    /// <summary>
    /// Emits <c>EventId 2139 OperationFailed</c> at <see cref="LogLevel.Error"/> with
    /// template <c>"Operation: [{OperationName}]. Status: [FAIL]. Duration: [{DurationMs} ms]. Message: [{Message}]"</c>.
    /// <paramref name="exception"/> is attached as the structured-logging exception, never embedded
    /// in the template — protects against <see cref="FormatException"/> when the message contains
    /// raw <c>{</c> / <c>}</c>.
    /// </summary>
    public static void OperationFailed(this ILogger logger, string operationName, long elapsedMilliseconds,
        Exception? exception = null, string? message = null)
    {
        OperationLogMessages.OperationFailed(
            logger,
            exception,
            operationName,
            elapsedMilliseconds,
            message ?? exception?.Message);
    }

    /// <summary>
    /// Emits <c>EventId 2138 OperationSucceeded</c> with template
    /// <c>"Operation: [{OperationName}]. Status: [SUCCESS]. Duration: [{DurationMs} ms]"</c>.
    /// When <paramref name="message"/> is supplied it is forwarded as a separate
    /// <c>EventId 2140</c> entry.
    /// </summary>
    public static void OperationSucceeded(this ILogger logger, string operationName, long elapsedMilliseconds,
        string? message = null)
    {
        if (message is not null)
        {
            OperationLogMessages.UserMessage(logger, message);
        }

        OperationLogMessages.OperationSucceeded(logger, operationName, elapsedMilliseconds);
    }
}



