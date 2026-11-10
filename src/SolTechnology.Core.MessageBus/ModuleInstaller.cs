﻿﻿using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Logging;
using SolTechnology.Core.MessageBus.Broker;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus
{
    /// <summary>
    /// Registers the Service Bus message bus: the publisher, the receiver hosted service, and the queue and topic endpoints.
    /// </summary>
    public static class ModuleInstaller
    {
        /// <param name="services">The service collection to add the message bus services to.</param>
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Registers <see cref="IMessagePublisher"/>, the receiver hosted service, and a dedicated <c>ServiceBusClient</c>
            /// built from <paramref name="messageBusConfiguration"/>. Chain <c>WithQueue*</c> and <c>WithTopic*</c> to add endpoints.
            /// </summary>
            /// <exception cref="ArgumentNullException"><paramref name="messageBusConfiguration"/> is <c>null</c>.</exception>
            public IServiceCollection AddSolMessageBus(MessageBusConfiguration messageBusConfiguration)
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
                        config.MaxDeliveryCount = messageBusConfiguration.MaxDeliveryCount;
                    })
                    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                        $"{nameof(MessageBusConfiguration)}.{nameof(MessageBusConfiguration.ConnectionString)} is required.")
                    .Validate(o => o.MaxDeliveryCount > 0,
                        $"{nameof(MessageBusConfiguration)}.{nameof(MessageBusConfiguration.MaxDeliveryCount)} must be greater than zero.")
                    .ValidateOnStart();

                services.TryAddSingleton<MessageBusRegistry>();

                services.AddSolCorrelationIdService();
                services.AddSolChronicle();

                services.TryAddKeyedSingleton<ServiceBusClient>(
                    MessageBusClientFactory.ServiceKey,
                    (sp, _) => MessageBusClientFactory.Create(sp.GetRequiredService<IOptions<MessageBusConfiguration>>().Value));
                services.TryAddSingleton<IMessageBusBroker>(sp => new MessageBusBroker(
                    sp.GetRequiredService<IOptions<MessageBusConfiguration>>(),
                    sp.GetRequiredService<MessageBusRegistry>(),
                    sp.GetRequiredKeyedService<ServiceBusClient>(MessageBusClientFactory.ServiceKey),
                    sp.GetRequiredService<ILogger<MessageBusBroker>>()));
                services.TryAddSingleton<IMessagePublisher, MessagePublisher>();

                services.AddHostedService<MessageBusReceiver>();

                return services;
            }

            /// <summary>Publishes <typeparamref name="TMessage"/> to the topic <paramref name="topicName"/>.</summary>
            public IServiceCollection WithTopicPublisher<TMessage>(string topicName) where TMessage : IMessage
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(topicName);

                EndpointRegistrar.Register(services, new EndpointRegistration(
                    typeof(TMessage), EndpointKind.Topic, EndpointRole.Publisher, topicName, SubscriptionName: null));

                return services;
            }

            /// <summary>
            /// Receives <typeparamref name="TMessage"/> from <paramref name="subscriptionName"/> on <paramref name="topicName"/>
            /// and registers <typeparamref name="THandler"/> as its scoped handler. Instances that share the subscription compete for messages.
            /// </summary>
            public IServiceCollection WithTopicReceiver<TMessage, THandler>(string topicName, string subscriptionName)
                where TMessage : IMessage where THandler : class, IMessageHandler<TMessage>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
                ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);

                services.AddScoped<IMessageHandler<TMessage>, THandler>();

                EndpointRegistrar.Register(services, new EndpointRegistration(
                    typeof(TMessage), EndpointKind.Topic, EndpointRole.Receiver, topicName, subscriptionName));

                return services;
            }

            /// <summary>
            /// Publishes <typeparamref name="TMessage"/> to <paramref name="queueName"/>, or to the queue configured for it in
            /// <see cref="MessageBusConfiguration.Queues"/> when the name is omitted.
            /// </summary>
            /// <exception cref="ArgumentException">No queue name is given or configured for <typeparamref name="TMessage"/>.</exception>
            public IServiceCollection WithQueuePublisher<TMessage>(string? queueName = null) where TMessage : IMessage
            {
                queueName = EndpointRegistrar.ResolveQueueName<TMessage>(services, queueName);

                EndpointRegistrar.Register(services, new EndpointRegistration(
                    typeof(TMessage), EndpointKind.Queue, EndpointRole.Publisher, queueName, SubscriptionName: null));

                return services;
            }

            /// <summary>
            /// Receives <typeparamref name="TMessage"/> from <paramref name="queueName"/>, or from the queue configured for it in
            /// <see cref="MessageBusConfiguration.Queues"/>, and registers <typeparamref name="THandler"/> as its scoped handler.
            /// </summary>
            /// <exception cref="ArgumentException">No queue name is given or configured for <typeparamref name="TMessage"/>.</exception>
            public IServiceCollection WithQueueReceiver<TMessage, THandler>(string? queueName = null)
                where TMessage : IMessage where THandler : class, IMessageHandler<TMessage>
            {
                queueName = EndpointRegistrar.ResolveQueueName<TMessage>(services, queueName);

                services.AddScoped<IMessageHandler<TMessage>, THandler>();

                EndpointRegistrar.Register(services, new EndpointRegistration(
                    typeof(TMessage), EndpointKind.Queue, EndpointRole.Receiver, queueName, SubscriptionName: null));

                return services;
            }
        }
    }
}
