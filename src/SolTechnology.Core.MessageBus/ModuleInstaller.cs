using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddMessageBus(
            this IServiceCollection services,
            MessageBusConfiguration messageBusConfiguration = null)
        {
            services
            .AddOptions<MessageBusConfiguration>()
            .Configure<IConfiguration>((config, configuration) =>
            {
                if (messageBusConfiguration == null)
                {
                    messageBusConfiguration = configuration.GetSection("Configuration:MessageBus").Get<MessageBusConfiguration>();
                }

                if (messageBusConfiguration == null)
                {
                    throw new ArgumentException($"The [{nameof(MessageBusConfiguration)}] is missing. Provide it by parameter or appsettings configuration section");
                }

                config.ConnectionString = messageBusConfiguration.ConnectionString;
                config.Queues = messageBusConfiguration.Queues;
            });

            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            services.AddSingleton<IMessageBusConfigurationProvider, MessageBusConfigurationProvider>();

            return services;
        }


        //TOPIC
        public static IServiceCollection WithTopicPublisher<TMessage>(
            this IServiceCollection services,
            string topicName) where TMessage : IMessage
        {
            var configurationProvider = services.BuildServiceProvider().GetRequiredService<IMessageBusConfigurationProvider>();

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
                .GetRequiredService<IMessageBusConfigurationProvider>();

            services.AddHostedService<MessageBusReceiver<TMessage>>();
            services.AddScoped<THandler>();
            services.AddScoped(typeof(MessageBusReceiver<TMessage>), (serviceProvider) => serviceProvider.GetRequiredService<THandler>());

            string messageType = typeof(TMessage).Name;

            configurationProvider.RegisterTopicReceiver(messageType, topicName, subscriptionName);

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

            var configurationProvider = services.BuildServiceProvider().GetRequiredService<IMessageBusConfigurationProvider>();


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
                .GetRequiredService<IMessageBusConfigurationProvider>();

            services.AddHostedService<MessageBusReceiver<TMessage>>();
            services.AddScoped<IMessageHandler<TMessage>,THandler>();
            // services.AddScoped(typeof(MessageBusReceiver<TMessage>), (serviceProvider) => serviceProvider.GetRequiredService<THandler>());



            configurationProvider.RegisterQueueReceiver(messageType, queueName);

            return services;
        }
    }
}
