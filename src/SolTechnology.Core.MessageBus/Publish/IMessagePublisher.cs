﻿namespace SolTechnology.Core.MessageBus.Publish;

/// <summary>
/// Publishes messages to the queues and topics registered with <c>WithQueuePublisher</c> and
/// <c>WithTopicPublisher</c>. Registered as a singleton by <c>AddSolMessageBus</c>.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Serialises and publishes <paramref name="message"/> to every Service Bus
    /// entity (queue/topic) registered for its concrete type.
    /// </summary>
    Task Publish(IMessage message, CancellationToken cancellationToken = default);
}

