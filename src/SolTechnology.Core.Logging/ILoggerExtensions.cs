using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging
{
    // ReSharper disable once InconsistentNaming
    public static class ILoggerExtensions
    {
        private static string OperationName { get; set; }
        private static Dictionary<string, object> OperationIdentifiers { get; set; } = new Dictionary<string, object>();


        public static IDisposable OperationStarted(this ILogger logger, string operationName, object operationIdentifiers = null)
        {
            OperationName = operationName;

            OperationIdentifiers = TypeDescriptor.GetProperties(operationIdentifiers)
                .OfType<PropertyDescriptor>()
                .ToDictionary(
                    prop => prop.Name,
                    prop => prop.GetValue(operationIdentifiers)
                );

            LogOperation(logger, "START");

            return logger.BeginScope(OperationIdentifiers);
        }

        public static void OperationFailed(this ILogger logger, Exception exception = null, string message = null)
        {
            if (exception != null)
            {
                logger.LogError(exception, message ?? exception.Message);
            }

            LogOperation(logger, "FAILURE");

            return;
        }

        public static void OperationSucceeded(this ILogger logger, string message = null)
        {
            if (message != null)
            {
                logger.LogInformation(message);
            }

            LogOperation(logger, "SUCCESS");

            return;
        }

        private static void LogOperation(this ILogger logger, string status)
        {
            string message = OperationName;

            var tempIdentifiers = OperationIdentifiers;
            tempIdentifiers.Add("STATUS", status);


            message += " Custom dimensions:";
            foreach (var keyValuePair in tempIdentifiers)
            {

                message += " {" + keyValuePair.Key + "}";
            }


            logger.LogInformation(2137, message, tempIdentifiers.Values.ToArray());
        }

    }
}