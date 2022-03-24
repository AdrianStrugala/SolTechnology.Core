using Azure.Messaging.ServiceBus;

namespace SolTechnology.Core.MessageBus.Configuration;

public interface IMessageBusConfigurationProvider
{
    void RegisterMessagePublisher(string messageType, string topicName);
    List<ServiceBusSender> ResolveMessagePublisher(string messageType);
    void RegisterMessageReceiver(string messageType, string topicName, string subscriptionName);
    List<ServiceBusProcessor> ResolveMessageReceiver(string messageType);
}