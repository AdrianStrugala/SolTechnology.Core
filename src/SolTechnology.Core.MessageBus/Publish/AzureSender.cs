using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System.Text;

namespace SolTechnology.Core.MessageBus.Publish
{
    public class AzureSender : ISender
    {
        private readonly ServiceBusSender _sender;

        public AzureSender(ServiceBusSender sender)
        {
            _sender = sender;
        }
        public async Task Send(IMessage message)
        {
            var azureMessage = BuildMessage(message);
            await _sender.SendMessageAsync(azureMessage);
        }

        public async Task Close()
        {
            await _sender.CloseAsync();
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
