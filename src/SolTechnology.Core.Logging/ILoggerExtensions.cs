using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging
{
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
            LogOperation(logger, operationName, "START");

            return;
        }

        public static void OperationFailed(this ILogger logger, string operationName, Exception exception = null, string message = null)
        {
            if (exception != null)
            {
                logger.LogError(exception, message ?? exception.Message);
            }

            LogOperation(logger, operationName, "FAILED");

            return;
        }

        public static void OperationSucceeded(this ILogger logger, string operationName, string message = null)
        {
            if (message != null)
            {
                logger.LogInformation(message);
            }

            LogOperation(logger, operationName, "SUCCEEDED");

            return;
        }

        private static void LogOperation(this ILogger logger, string operationName, string status)
        {
            string message = "Operation: [{operationName}]. Status: [{status}]";

            logger.LogInformation(2137, message, operationName, status);
        }

    }
}