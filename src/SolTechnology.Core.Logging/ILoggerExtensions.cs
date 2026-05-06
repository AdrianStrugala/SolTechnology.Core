using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging;

// ReSharper disable once InconsistentNaming
public static class ILoggerExtensions
{
    public static void OperationStarted(this ILogger logger, string operationName, string? message = null)
    {
        if (message is not null)
        {
            // 'message' is user input - never use it as a message template (curly braces break the formatter).
            OperationLogMessages.UserMessage(logger, message);
        }

        OperationLogMessages.OperationStarted(logger, operationName);
    }

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


