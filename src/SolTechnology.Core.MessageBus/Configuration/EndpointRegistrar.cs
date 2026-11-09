using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.MessageBus.Configuration;

/// <summary>
/// Adds endpoints to the single <see cref="MessageBusRegistry"/> instance of a service collection and finds the queue
/// configured for a message type in <see cref="MessageBusConfiguration.Queues"/>, without building a service provider.
/// </summary>
internal static class EndpointRegistrar
{
    public static void Register(IServiceCollection services, EndpointRegistration endpoint) =>
        GetRegistry(services).Add(endpoint);

    public static string ResolveQueueName<TMessage>(IServiceCollection services, string? queueName)
    {
        if (!string.IsNullOrWhiteSpace(queueName))
        {
            return queueName;
        }

        var messageType = typeof(TMessage).Name;

        var probe = new MessageBusConfiguration();
        foreach (var descriptor in services.Where(d =>
                     d.ServiceType == typeof(IConfigureOptions<MessageBusConfiguration>)))
        {
            if (descriptor.ImplementationInstance is IConfigureOptions<MessageBusConfiguration> instance)
            {
                instance.Configure(probe);
            }
        }

        queueName = probe.Queues.FirstOrDefault(q => q.MessageType == messageType)?.QueueName;

        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException(
                $"Queue name for message type [{messageType}] is missing. " +
                "Provide it as the parameter or configure it under " +
                $"{nameof(MessageBusConfiguration)}.{nameof(MessageBusConfiguration.Queues)}.");
        }

        return queueName;
    }

    private static MessageBusRegistry GetRegistry(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MessageBusRegistry));
        if (descriptor?.ImplementationInstance is MessageBusRegistry existing)
        {
            return existing;
        }

        // One instance shared by every With* call replaces the type registration, so DI resolves the filled registry.
        var registry = new MessageBusRegistry();
        services.RemoveAll<MessageBusRegistry>();
        services.AddSingleton(registry);
        return registry;
    }
}
