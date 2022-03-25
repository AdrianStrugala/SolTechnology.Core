using System.Text;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.Publish
{
    public class MessagePublisher : IMessagePublisher
    {
        private readonly IMessageBusConfigurationProvider _messageBusConfigurationProvider;

        public MessagePublisher(IMessageBusConfigurationProvider messageBusConfigurationProvider)
        {
            _messageBusConfigurationProvider = messageBusConfigurationProvider;
        }

        public async Task Publish(IMessage message)
        {
            var senders = _messageBusConfigurationProvider.ResolveMessagePublisher(message.GetType().Name);

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
}
