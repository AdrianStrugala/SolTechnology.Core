﻿﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SolTechnology.Core.MessageBus.Broker;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddSolMessageBus(
            this IServiceCollection services,
            MessageBusConfiguration messageBusConfiguration)
        {
            ArgumentNullException.ThrowIfNull(messageBusConfiguration);

            services
                .AddOptions<MessageBusConfiguration>()
                .Configure(config =>
                {
                    config.ConnectionString = messageBusConfiguration.ConnectionString;
                    config.Queues = messageBusConfiguration.Queues;
                    config.CreateResources = messageBusConfiguration.CreateResources;
                    config.TransportType = messageBusConfiguration.TransportType;
                    config.MaxConcurrentCalls = messageBusConfiguration.MaxConcurrentCalls;
                    config.PrefetchCount = messageBusConfiguration.PrefetchCount;
                    config.RetryOptions = messageBusConfiguration.RetryOptions;
                })
                .ValidateOnStart();

            // Registry is a singleton populated synchronously during DI configuration.
            services.TryAddSingleton<MessageBusRegistry>();

            services.TryAddSingleton<IMessageBusBroker, MessageBusBroker>();
            services.TryAddSingleton<IMessagePublisher, MessagePublisher>();

            services.AddHostedService<MessageBusReceiver>();

            return services;
        }

        // ---- TOPIC ----
        public static IServiceCollection WithTopicPublisher<TMessage>(
            this IServiceCollection services,
            string topicName) where TMessage : IMessage
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topicName);

            GetRegistry(services).Add(new EndpointRegistration(
                typeof(TMessage), EndpointKind.Topic, EndpointRole.Publisher, topicName, SubscriptionName: null));

            return services;
        }

        public static IServiceCollection WithTopicReceiver<TMessage, THandler>(
            this IServiceCollection services,
            string topicName,
            string subscriptionName)
            where TMessage : IMessage where THandler : class, IMessageHandler<TMessage>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
            ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);

            services.AddScoped<IMessageHandler<TMessage>, THandler>();

            GetRegistry(services).Add(new EndpointRegistration(
                typeof(TMessage), EndpointKind.Topic, EndpointRole.Receiver, topicName, subscriptionName));

            return services;
        }

        // ---- QUEUE ----
        public static IServiceCollection WithQueuePublisher<TMessage>(
            this IServiceCollection services,
            string? queueName = null) where TMessage : IMessage
        {
            queueName = ResolveQueueName<TMessage>(services, queueName);

            GetRegistry(services).Add(new EndpointRegistration(
                typeof(TMessage), EndpointKind.Queue, EndpointRole.Publisher, queueName, SubscriptionName: null));

            return services;
        }

        public static IServiceCollection WithQueueReceiver<TMessage, THandler>(
            this IServiceCollection services,
            string? queueName = null)
            where TMessage : IMessage where THandler : class, IMessageHandler<TMessage>
        {
            queueName = ResolveQueueName<TMessage>(services, queueName);

            services.AddScoped<IMessageHandler<TMessage>, THandler>();

            GetRegistry(services).Add(new EndpointRegistration(
                typeof(TMessage), EndpointKind.Queue, EndpointRole.Receiver, queueName, SubscriptionName: null));

            return services;
        }

        private static string ResolveQueueName<TMessage>(IServiceCollection services, string? queueName)
        {
            if (!string.IsNullOrWhiteSpace(queueName)) return queueName;

            var messageType = typeof(TMessage).Name;

            // Read the configured queue list directly from registered IConfigureOptions
            // instances without spinning a temporary ServiceProvider.
            var probe = new MessageBusConfiguration();
            foreach (var d in services.Where(d =>
                         d.ServiceType == typeof(IConfigureOptions<MessageBusConfiguration>)))
            {
                if (d.ImplementationInstance is IConfigureOptions<MessageBusConfiguration> instance)
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

            // First registration: replace the TryAddSingleton<MessageBusRegistry>()
            // (registered by AddSolMessageBus) with a concrete singleton instance so
            // that all WithQueue*/WithTopic* calls collaborate on the same object,
            // and the DI container later resolves THIS instance.
            var registry = new MessageBusRegistry();
            services.RemoveAll<MessageBusRegistry>();
            services.AddSingleton(registry);
            return registry;
        }
    }
}

