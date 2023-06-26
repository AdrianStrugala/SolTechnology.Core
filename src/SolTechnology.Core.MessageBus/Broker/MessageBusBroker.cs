using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Options;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.Broker
{
    public class MessageBusBroker : IMessageBusBroker, IDisposable
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly bool _createResources;
        private readonly ManagementClient _managementClient;

        private static readonly List<(string, ServiceBusSender)> MessageToSenderMap = new();
        private static readonly List<(Type, ServiceBusProcessor)> MessageToProcessorMap = new();



        //Service bus client options can be added here
        public MessageBusBroker(IOptions<MessageBusConfiguration> options)
        {
            var connectionString = options.Value.ConnectionString;
            _createResources = options.Value.CreateResources;

            var clientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpTcp
            };

            _serviceBusClient = new ServiceBusClient(connectionString, clientOptions);

            if (_createResources)
            {
                _managementClient = new ManagementClient(connectionString);
            }
        }

        //Topic descriptions can be added here
        public void RegisterTopicPublisher(string messageType, string topicName)
        {
            var topicSender = _serviceBusClient.CreateSender(topicName);
            MessageToSenderMap.Add((messageType, topicSender));

            if (_createResources)
            {
                if (!_managementClient.TopicExistsAsync(topicName).GetAwaiter().GetResult())
                {
                    _managementClient.CreateTopicAsync(topicName).GetAwaiter().GetResult();
                }
            }
        }

        public List<ServiceBusSender> ResolveMessagePublisher(string messageType)
        {
            var senders = MessageToSenderMap
                .Where(m => m.Item1.Equals(messageType, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.Item2)
                .ToList();

            if (!senders.Any())
            {
                throw new ArgumentException($"No message bus Publisher for Message: [{messageType}] is configured.");
            }

            return senders;
        }

        //Subscription Options can be added here (MaxAutoLockRenewalDuration, MaxConcurrentCalls)
        public void RegisterTopicReceiver(Type messageType, string topicName, string subscriptionName)
        {
            var serviceBusProcessorOptions = new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = true,
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };

            ServiceBusProcessor serviceBusProcessor = _serviceBusClient.CreateProcessor(topicName, subscriptionName, serviceBusProcessorOptions);
            MessageToProcessorMap.Add((messageType, serviceBusProcessor));

            if (_createResources)
            {
                if (!_managementClient.SubscriptionExistsAsync(topicName, subscriptionName).GetAwaiter().GetResult())
                {
                    _managementClient.CreateSubscriptionAsync(topicName, subscriptionName).GetAwaiter().GetResult();
                }
            }
        }

        public List<(Type, ServiceBusProcessor)> ResolveMessageReceivers()
        {
            return MessageToProcessorMap;
        }

        public List<ServiceBusProcessor> ResolveMessageReceiver(string messageType)
        {
            var processors = MessageToProcessorMap
                .Where(m => m.Item1.Name.Equals(messageType, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.Item2)
                .ToList();

            if (!processors.Any())
            {
                throw new ArgumentException($"No message bus Receiver for Message: [{messageType}] is configured.");
            }

            return processors;
        }

        public void RegisterQueuePublisher(string messageType, string queueName)
        {
            var queueSender = _serviceBusClient.CreateSender(queueName);
            MessageToSenderMap.Add((messageType, queueSender));

            if (_createResources)
            {
                if (!_managementClient.QueueExistsAsync(queueName).GetAwaiter().GetResult())
                {
                    _managementClient.CreateQueueAsync(queueName).GetAwaiter().GetResult();
                }
            }
        }

        public void RegisterQueueReceiver(Type messageType, string queueName)
        {
            var serviceBusProcessorOptions = new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = true,
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };

            ServiceBusProcessor serviceBusProcessor = _serviceBusClient.CreateProcessor(queueName, serviceBusProcessorOptions);
            MessageToProcessorMap.Add((messageType, serviceBusProcessor));

            if (_createResources)
            {
                if (!_managementClient.QueueExistsAsync(queueName).GetAwaiter().GetResult())
                {
                    _managementClient.CreateQueueAsync(queueName).GetAwaiter().GetResult();
                }
            }
        }


        public void Dispose()
        {
            _serviceBusClient.DisposeAsync().GetAwaiter().GetResult();
        }
    }
}