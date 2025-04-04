﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SolTechnology.Core.MessageBus.Broker;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddMessageBus(
            this IServiceCollection services,
            MessageBusConfiguration messageBusConfiguration)
        {
            if (messageBusConfiguration == null)
            {
                throw new ArgumentException($"The [{nameof(MessageBusConfiguration)}] is missing. Provide it by parameter.");
            }

            services
            .AddOptions<MessageBusConfiguration>()
            .Configure(config =>
            {
                config.ConnectionString = messageBusConfiguration.ConnectionString;
                config.Queues = messageBusConfiguration.Queues;
                config.CreateResources = messageBusConfiguration.CreateResources;
            });

            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            services.AddSingleton<IMessageBusBroker, MessageBusBroker>();

            services.AddHostedService<MessageBusReceiver>();

            return services;
        }

        //TOPIC
        public static IServiceCollection WithTopicPublisher<TMessage>(
            this IServiceCollection services,
            string topicName) where TMessage : IMessage
        {
            var configurationProvider = services.BuildServiceProvider().GetRequiredService<IMessageBusBroker>();

            string messageType = typeof(TMessage).Name;

            configurationProvider.RegisterTopicPublisher(messageType, topicName);

            return services;
        }

        public static IServiceCollection WithTopicReceiver<TMessage, THandler>(
            this IServiceCollection services,
            string topicName,
            string subscriptionName)
            where TMessage : IMessage where THandler : class, IMessageHandler<TMessage>
        {
            var configurationProvider = services.BuildServiceProvider()
                .GetRequiredService<IMessageBusBroker>();

            services.AddScoped<IMessageHandler<TMessage>, THandler>();

            string messageType = typeof(TMessage).Name;

            configurationProvider.RegisterTopicReceiver(typeof(TMessage), topicName, subscriptionName);

            return services;
        }

        //QUEUE
        public static IServiceCollection WithQueuePublisher<TMessage>(
            this IServiceCollection services,
            string queueName = null) where TMessage : IMessage
        {
            string messageType = typeof(TMessage).Name;

            if (queueName == null)
            {
                var options = services.BuildServiceProvider().GetRequiredService<IOptions<MessageBusConfiguration>>().Value;
                queueName = options.Queues.FirstOrDefault(q => q.MessageType == messageType)?.QueueName;
            }

            if (queueName == null)
            {
                throw new ArgumentException($"The [{nameof(queueName)}] for message type: [{messageType}]is missing. Provide it by parameter or appsettings configuration section");
            }

            var configurationProvider = services.BuildServiceProvider().GetRequiredService<IMessageBusBroker>();


            configurationProvider.RegisterQueuePublisher(messageType, queueName);

            return services;
        }

        public static IServiceCollection WithQueueReceiver<TMessage, THandler>(
            this IServiceCollection services,
            string queueName = null)
            where TMessage : IMessage where THandler : class, IMessageHandler<TMessage>
        {
            string messageType = typeof(TMessage).Name;

            if (queueName == null)
            {
                var options = services.BuildServiceProvider().GetRequiredService<IOptions<MessageBusConfiguration>>().Value;
                queueName = options.Queues.FirstOrDefault(q => q.MessageType == messageType)?.QueueName;
            }

            if (queueName == null)
            {
                throw new ArgumentException($"The [{nameof(queueName)}] is missing for message type: [{messageType}] is missing. Provide it by parameter or appsettings configuration section");
            }

            var configurationProvider = services.BuildServiceProvider()
                .GetRequiredService<IMessageBusBroker>();

            services.AddScoped<IMessageHandler<TMessage>, THandler>();

            configurationProvider.RegisterQueueReceiver(typeof(TMessage), queueName);

            return services;
        }
    }
}
