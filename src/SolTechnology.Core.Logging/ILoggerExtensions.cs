using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging;

// ReSharper disable once InconsistentNaming
public static class ILoggerExtensions
{

    public static void OperationStarted(this ILogger logger, string operationName, string? message = null)
    {
        if (message != null)
        {
            // 'message' is user input - never use it as a message template (curly braces break the formatter).
            logger.LogInformation("{Message}", message);
        }

        logger.LogInformation(
            new EventId(2137, nameof(OperationStarted)),
            "Operation: [{OperationName}]. Status: [{Status}]",
            operationName, "START");
    }

    public static void OperationFailed(this ILogger logger, string operationName, long elapsedMilliseconds,
        Exception? exception = null, string? message = null)
    {
        // Never pass an exception/user message as a message template - any '{' / '}'
        // in the text breaks the structured-logging formatter (FormatException at runtime).
        // Pass it as a templated argument instead.
        logger.Log(
            LogLevel.Error,
            new EventId(2139, nameof(OperationFailed)),
            exception,
            "Operation: [{OperationName}]. Status: [{Status}]. Duration: [{DurationMs} ms]. Message: [{Message}]",
            operationName,
            "FAIL",
            elapsedMilliseconds,
            message ?? exception?.Message);
    }

    public static void OperationSucceeded(this ILogger logger, string operationName, long elapsedMilliseconds,
        string? message = null)
    {
        if (message != null)
        {
            logger.LogInformation("[{Message}]", message);
        }

        logger.LogInformation(
            new EventId(2138, nameof(OperationSucceeded)),
            "Operation: [{OperationName}]. Status: [{Status}]. Duration: [{DurationMs} ms]",
            operationName, "SUCCESS", elapsedMilliseconds);
    }
}
