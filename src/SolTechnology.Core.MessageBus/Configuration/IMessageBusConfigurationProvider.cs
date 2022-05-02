using Azure.Messaging.ServiceBus;

namespace SolTechnology.Core.MessageBus.Configuration;

public interface IMessageBusConfigurationProvider
{
    void RegisterTopicPublisher(string messageType, string topicName);
    List<ServiceBusSender> ResolveMessagePublisher(string messageType);
    void RegisterTopicReceiver(string messageType, string topicName, string subscriptionName);
    List<ServiceBusProcessor> ResolveMessageReceiver(string messageType);

    void RegisterQueuePublisher(string messageType, string queueName);
    void RegisterQueueReceiver(string messageType, string queueName);
}