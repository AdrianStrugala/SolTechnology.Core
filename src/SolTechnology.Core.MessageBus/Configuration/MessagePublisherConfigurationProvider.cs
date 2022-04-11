using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.MessageBus.Configuration
{
    public class MessageBusConfigurationProvider : IMessageBusConfigurationProvider
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ManagementClient _managementClient;

        private static readonly List<(string, ServiceBusSender)> MessageToSenderMap = new();
        private static readonly List<(string, ServiceBusProcessor)> MessageToProcessorMap = new();


        //Service bus client options can be added here
        public MessageBusConfigurationProvider(IOptions<MessageBusConfiguration> options)
        {
            var connectionString = options.Value.ConnectionString;

            _serviceBusClient = new ServiceBusClient(connectionString);
            _managementClient = new ManagementClient(connectionString);

        }

        //Topic descriptions can be added here
        public void RegisterMessagePublisher(string messageType, string topicName)
        {
            var topicSender = _serviceBusClient.CreateSender(topicName);
            MessageToSenderMap.Add((messageType, topicSender));

            if (!_managementClient.TopicExistsAsync(topicName).GetAwaiter().GetResult())
            {
                _managementClient.CreateTopicAsync(topicName).GetAwaiter().GetResult();
            }
        }

        public List<ServiceBusSender> ResolveMessagePublisher(string messageType)
        {
            var senders = MessageToSenderMap
                .Where(m => m.Item1.Equals(messageType, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.Item2)
                .ToList();

            if (!senders.Any())
            {
                throw new ArgumentException($"Message bus topic for Topic: [{messageType}] is not configured.");
            }

            return senders;
        }

        //Subscription Options can be added here (MaxAutoLockRenewalDuration, MaxConcurrentCalls)
        public void RegisterMessageReceiver(string messageType, string topicName, string subscriptionName)
        {
            var serviceBusProcessorOptions = new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = true,
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };

            ServiceBusProcessor serviceBusProcessor = _serviceBusClient.CreateProcessor(topicName, subscriptionName, serviceBusProcessorOptions);
            MessageToProcessorMap.Add((messageType, serviceBusProcessor));

            if (!_managementClient.SubscriptionExistsAsync(topicName, subscriptionName).GetAwaiter().GetResult())
            {
                _managementClient.CreateSubscriptionAsync(topicName, subscriptionName).GetAwaiter().GetResult();
            }
        }

        public List<ServiceBusProcessor> ResolveMessageReceiver(string messageType)
        {
            var processors = MessageToProcessorMap
                .Where(m => m.Item1.Equals(messageType, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.Item2)
                .ToList();

            if (!processors.Any())
            {
                throw new ArgumentException($"Message bus topic for Topic: [{messageType}] is not configured.");
            }

            return processors;
        }
    }
}