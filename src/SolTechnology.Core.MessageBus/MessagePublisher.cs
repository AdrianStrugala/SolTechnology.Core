using System.Text;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;


namespace SolTechnology.Core.MessageBus
{
    public interface IMessagePublisher
    {
        Task Publish(IMessage message);
    }

    public class MessagePublisher : IMessagePublisher
    {
        private readonly IMessagePublisherConfigurationProvider _messagePublisherConfigurationProvider;

        public MessagePublisher(IMessagePublisherConfigurationProvider messagePublisherConfigurationProvider)
        {
            _messagePublisherConfigurationProvider = messagePublisherConfigurationProvider;
        }

        public async Task Publish(IMessage message)
        {
            var senders = _messagePublisherConfigurationProvider.ResolveMessagePublisher(message.GetType().Name);

            var serviceBusMessage = BuildMessage(message);


            foreach (var sender in senders)
            {
                await sender.SendMessageAsync(serviceBusMessage);
            }
        }


        private static ServiceBusMessage BuildMessage(IMessage message)
        {
            var serializedMessage = JsonConvert.SerializeObject(message);
            var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(serializedMessage));
            serviceBusMessage.ContentType = "application/json";
            serviceBusMessage.MessageId = message.Id;
            serviceBusMessage.ApplicationProperties["Type"] = message.GetType().Name;
            return serviceBusMessage;
        }
    }

    public interface IMessage
    {
        string Id => Guid.NewGuid().ToString();
    }
}
