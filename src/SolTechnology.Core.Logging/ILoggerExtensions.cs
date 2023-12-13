using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging;

// ReSharper disable once InconsistentNaming
public static class ILoggerExtensions
{
    public static IDisposable BeginOperationScope(this ILogger logger, object operationIdentifiers)
    {
        var operationIdentifiersDictionary = TypeDescriptor.GetProperties(operationIdentifiers)
            .OfType<PropertyDescriptor>()
            .ToDictionary(
                prop => prop.Name,
                prop => prop.GetValue(operationIdentifiers)
            );

        return logger.BeginScope(operationIdentifiersDictionary);
    }

    public static IDisposable BeginOperationScope(this ILogger logger, KeyValuePair<string, object> operationIdentifier)
    {
        var scopeDictionary = new Dictionary<string, object>
        {
            { operationIdentifier.Key, operationIdentifier.Value }
        };

        return logger.BeginScope(scopeDictionary);
    }

    public static IDisposable BeginOperationScope(this ILogger logger, Dictionary<string, object> operationIdentifiers)
    {
        return logger.BeginScope(operationIdentifiers);
    }

    public static void OperationStarted(this ILogger logger, string operationName, string message = null)
    {
        if (message != null)
        {
            logger.LogInformation(message);
        }
        logger.LogInformation(2137, "Operation: [{operationName}]. Status: [{status}]",
            operationName, "START");

        return;
    }

    public static void OperationFailed(this ILogger logger, string operationName, long elapsedMilliseconds,
        Exception exception = null, string message = null)
    {
        if (exception != null)
        {
            logger.LogError(exception, message ?? exception.Message);
        }

        logger.LogInformation(2137, "Operation: [{operationName}]. Status: [{status}]. Duration: [{duration}]",
            operationName, "FAIL", elapsedMilliseconds);

        return;
    }

    public static void OperationSucceeded(this ILogger logger, string operationName, long elapsedMilliseconds,
        string message = null)
    {
        if (message != null)
        {
            logger.LogInformation(message);
        }

        logger.LogInformation(2137, "Operation: [{operationName}]. Status: [{status}]. Duration: [{duration}]",
            operationName, "SUCCESS", elapsedMilliseconds);
    }
}