﻿namespace SolTechnology.Core.MessageBus.Publish;

public interface IMessagePublisher
{
    /// <summary>
    /// Serialises and publishes <paramref name="message"/> to every Service Bus
    /// entity (queue/topic) registered for its concrete type.
    /// </summary>
    Task Publish(IMessage message, CancellationToken cancellationToken = default);
}

