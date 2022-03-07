using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging
{
    // ReSharper disable once InconsistentNaming
    public static class ILoggerExtensions
    {
        public static IDisposable OperationStarted(this ILogger logger, string operationName, object operationIdentifiers)
        {
            var operationNameDictionary = TypeDescriptor.GetProperties(operationIdentifiers)
                .OfType<PropertyDescriptor>()
                .ToDictionary(
                    prop => prop.Name,
                    prop => prop.GetValue(operationIdentifiers)
                );

            using var beginScope = logger.BeginScope(operationNameDictionary);
            {
                LogOperation(logger, operationName, "START");
                return beginScope;
            }
        }

        public static void OperationFailed(this ILogger logger, string operationName, Exception exception = null, string message = null)
        {
            if (exception != null)
            {
                logger.LogError(exception, message ?? exception.Message);
            }

            LogOperation(logger, operationName, "FAILURE");

            return;
        }

        public static void OperationSucceeded(this ILogger logger, string operationName, string message = null)
        {
            if (message != null)
            {
                logger.LogInformation(message);
            }

            LogOperation(logger, operationName, "SUCCESS");

            return;
        }

        private static void LogOperation(this ILogger logger, string operationName, string status)
        {
            string message = "Operation: [{operationName}]. Status: [{status}]";

            logger.LogInformation(2137, message, operationName, status);
        }

    }
}