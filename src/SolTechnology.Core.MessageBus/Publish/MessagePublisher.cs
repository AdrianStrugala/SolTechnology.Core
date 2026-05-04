using System.Net.Mime;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SolTechnology.Core.MessageBus.Broker;

namespace SolTechnology.Core.MessageBus.Publish
{
    public sealed class MessagePublisher : IMessagePublisher
    {
        private readonly IMessageBusBroker _messageBusBroker;
        private readonly ILogger<MessagePublisher> _logger;

        public MessagePublisher(
            IMessageBusBroker messageBusBroker,
            ILogger<MessagePublisher> logger)
        {
            _messageBusBroker = messageBusBroker;
            _logger = logger;
        }

        public async Task Publish(IMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            await _messageBusBroker.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            var messageType = message.GetType();
            var senders = _messageBusBroker.ResolveSenders(messageType);

            // Each sender requires its own ServiceBusMessage instance — reusing the
            // same instance after a successful send is unsupported by the SDK
            // (the underlying AMQP message is consumed).
            foreach (var sender in senders)
            {
                var serviceBusMessage = BuildMessage(message, messageType);
                try
                {
                    await sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
                    _logger.LogDebug(
                        "Published message {MessageType} {MessageId} to {Entity}",
                        messageType.Name, serviceBusMessage.MessageId, sender.EntityPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to publish message {MessageType} {MessageId} to {Entity}",
                        messageType.Name, serviceBusMessage.MessageId, sender.EntityPath);
                    throw;
                }
            }
        }

        internal static ServiceBusMessage BuildMessage(IMessage message, Type messageType)
        {
            var serializedMessage = JsonConvert.SerializeObject(message);
            var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(serializedMessage))
            {
                ContentType = MediaTypeNames.Application.Json,
                // Prefer the application-supplied id (drives ASB duplicate detection)
                // but fall back to a fresh GUID so a buggy or missing implementation
                // can never publish two distinct messages with the same MessageId.
                MessageId = string.IsNullOrWhiteSpace(message.Id) ? Guid.NewGuid().ToString() : message.Id,
                Subject = messageType.Name
            };
            serviceBusMessage.ApplicationProperties["Type"] = messageType.Name;
            return serviceBusMessage;
        }
    }
}
