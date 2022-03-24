using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.MessageBus.Configuration
{
    public class MessageBusConfigurationProvider : IMessageBusConfigurationProvider
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ManagementClient _managementClient;

        private static readonly List<(string, ServiceBusSender)> _messageToSenderMap = new();
        private static readonly List<(string, ServiceBusProcessor)> _messageToProcessorMap = new();

        //TODO add logs here

        public MessageBusConfigurationProvider(IOptions<MessageBusConfiguration> options)
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

        public void RegisterMessageReceiver(string messageType, string topicName, string subscriptionName)
        {
            var serviceBusProcessorOptions = new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = true,
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };


            ServiceBusProcessor serviceBusProcessor = _serviceBusClient.CreateProcessor(topicName, subscriptionName, serviceBusProcessorOptions);
            _messageToProcessorMap.Add((messageType, serviceBusProcessor));


            //TODO: add subscriptionOptions
            // if (messageBusSubscription.MaxAutoLockRenewalDuration.HasValue)
            // {
            //     serviceBusProcessorOptions.MaxAutoLockRenewalDuration =
            //         TimeSpan.FromMinutes(messageBusSubscription.MaxAutoLockRenewalDuration.Value);
            // }
            //
            // if (messageBusSubscription.MaxConcurrentCalls.HasValue)
            // {
            //     serviceBusProcessorOptions.MaxConcurrentCalls = messageBusSubscription.MaxConcurrentCalls.Value;
            // }

            if (!_managementClient.SubscriptionExistsAsync(topicName, subscriptionName).GetAwaiter().GetResult())
            {
                _managementClient.CreateSubscriptionAsync(topicName, subscriptionName).GetAwaiter().GetResult();
            }


        }

        public List<ServiceBusProcessor> ResolveMessageReceiver(string messageType)
        {
            var processors = _messageToProcessorMap
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