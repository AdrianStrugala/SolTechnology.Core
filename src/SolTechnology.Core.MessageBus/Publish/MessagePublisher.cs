﻿using System.Net.Mime;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.MessageBus.Broker;

namespace SolTechnology.Core.MessageBus.Publish
{
    public sealed class MessagePublisher(
        IMessageBusBroker messageBusBroker,
        ICorrelationIdService correlationIdService,
        ILogger<MessagePublisher> logger) : IMessagePublisher
    {
        public async Task Publish(IMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            await messageBusBroker.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            var messageType = message.GetType();
            var senders = messageBusBroker.ResolveSenders(messageType);

            foreach (var sender in senders)
            {
                var serviceBusMessage = BuildMessage(message, messageType);

                // Propagate correlation so the receiver can continue the trace.
                var correlation = correlationIdService.Get();
                if (correlation is not null)
                {
                    serviceBusMessage.ApplicationProperties["CorrelationId"] = correlation.Value;
                }

                try
                {
                    await sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
                    logger.LogDebug(
                        "Published message {MessageType} {MessageId} to {Entity}",
                        messageType.Name, serviceBusMessage.MessageId, sender.EntityPath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
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
