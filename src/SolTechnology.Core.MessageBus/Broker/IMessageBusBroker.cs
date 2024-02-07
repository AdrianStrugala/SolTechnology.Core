using Azure.Messaging.ServiceBus;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Broker;

public interface IMessageBusBroker
{
    void RegisterTopicPublisher(string messageType, string topicName);
    void RegisterTopicReceiver(Type messageType, string topicName, string subscriptionName);

    void RegisterQueuePublisher(string messageType, string queueName);
    void RegisterQueueReceiver(Type messageType, string queueName);


    List<ISender> ResolveMessagePublisher(string messageType);
    List<IReceiver> ResolveMessageReceiver(string messageType);
    List<(Type, IReceiver)> ResolveMessageReceivers();

}