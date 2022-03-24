using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                config.Publishers = messageBusConfiguration.Publishers;
            });

            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            services.AddSingleton<IMessageBusConfigurationProvider, MessageBusConfigurationProvider>();

            return services;
        }

        public static IServiceCollection WithPublisher<T>(
            this IServiceCollection services,
            string topicName) where T : IMessage
        {
            var configurationProvider = services.BuildServiceProvider().GetRequiredService<IMessageBusConfigurationProvider>();

            string messageType = typeof(T).Name;

            configurationProvider.RegisterMessagePublisher(messageType, topicName);

            return services;
        }

        public static IServiceCollection WithReceiver<TMessage, THandler>(
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

            configurationProvider.RegisterMessageReceiver(messageType, topicName, subscriptionName);

            return services;
        }
    }
}
