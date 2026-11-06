using Azure.Messaging.ServiceBus;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.Broker;

/// <summary>
/// Builds the <see cref="ServiceBusClient"/> that MessageBus shares between its broker and health check.
/// The client is registered under <see cref="ServiceKey"/>, so a consumer's own unkeyed client is never reused.
/// </summary>
internal static class MessageBusClientFactory
{
    internal const string ServiceKey = "SolTechnology.Core.MessageBus";

    internal static ServiceBusClient Create(MessageBusConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
        {
            throw new ArgumentException(
                $"{nameof(MessageBusConfiguration)}.{nameof(MessageBusConfiguration.ConnectionString)} is required.",
                nameof(configuration));
        }

        return new ServiceBusClient(configuration.ConnectionString, CreateOptions(configuration));
    }

    internal static ServiceBusClientOptions CreateOptions(MessageBusConfiguration configuration)
    {
        var clientOptions = new ServiceBusClientOptions
        {
            TransportType = configuration.TransportType
        };
        if (configuration.RetryOptions is not null)
        {
            clientOptions.RetryOptions = configuration.RetryOptions;
        }

        return clientOptions;
    }
}
