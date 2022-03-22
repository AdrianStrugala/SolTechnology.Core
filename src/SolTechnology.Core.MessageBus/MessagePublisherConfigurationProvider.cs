using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.MessageBus
{
    public interface IMessagePublisherConfigurationProvider
    {
        void RegisterMessagePublisher(string messageType, string topicName);
        List<ServiceBusSender> ResolveMessagePublisher(string messageType);
    }

    public class MessagePublisherConfigurationProvider : IMessagePublisherConfigurationProvider
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ManagementClient _managementClient;

        private static readonly List<(string, ServiceBusSender)> _messageToSenderMap = new();


        public MessagePublisherConfigurationProvider(IOptions<MessageBusConfiguration> options)
        {
            var connectionString = options.Value.ConnectionString;

            _serviceBusClient = new ServiceBusClient(connectionString);
            _managementClient = new ManagementClient(connectionString);
        }

        //TODO: Add here topic options (message time to live, retry count and so on)

        public void RegisterMessagePublisher(string messageType, string topicName)
        {
            var topicSender = _serviceBusClient.CreateSender(topicName);
            _messageToSenderMap.Add((messageType, topicSender));

            if (!_managementClient.TopicExistsAsync(topicName).GetAwaiter().GetResult())
            {
                _managementClient.CreateTopicAsync(topicName).GetAwaiter().GetResult();
            }
        }

        public List<ServiceBusSender> ResolveMessagePublisher(string messageType)
        {
            var senders = _messageToSenderMap
                .Where(m => m.Item1.Equals(messageType, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.Item2)
                .ToList();

            if (!senders.Any())
            {
                throw new ArgumentException($"Message bus topic for Topic: [{messageType}] is not configured.");
            }

            return senders;
        }
    }
}