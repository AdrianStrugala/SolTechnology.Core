using Azure.Messaging.ServiceBus;

namespace SolTechnology.Core.MessageBus.Broker;

public interface IMessageBusBroker
{
    void RegisterTopicPublisher(string messageType, string topicName);
    List<ServiceBusSender> ResolveMessagePublisher(string messageType);
    void RegisterTopicReceiver(Type messageType, string topicName, string subscriptionName);
    List<ServiceBusProcessor> ResolveMessageReceiver(string messageType);
    List<(Type, ServiceBusProcessor)> ResolveMessageReceivers();

    void RegisterQueuePublisher(string messageType, string queueName);
    void RegisterQueueReceiver(Type messageType, string queueName);
}