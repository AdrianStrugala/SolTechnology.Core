using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            services.AddSingleton<IMessagePublisherConfigurationProvider, MessagePublisherConfigurationProvider>();

            return services;
        }

        public static IServiceCollection WithPublisher<T>(
            this IServiceCollection services,
            string topicName) where T : IMessage
        {
            var publisher = services.BuildServiceProvider().GetRequiredService<IMessagePublisherConfigurationProvider>();

            string messageType = typeof(T).Name;

            publisher.RegisterMessagePublisher(messageType, topicName);

            return services;
        }
    }
}
