﻿namespace SolTechnology.Core.MessageBus.Configuration;

/// <summary>
/// Maps a message type to its queue, so <c>WithQueuePublisher</c> and <c>WithQueueReceiver</c> can be called
/// without a queue name.
/// </summary>
public class QueueConfiguration
{
    /// <summary>Name of the Service Bus queue.</summary>
    public string QueueName { get; set; } = string.Empty;
    /// <summary>Simple class name (<c>Type.Name</c>, without namespace) of the message type sent to the queue.</summary>
    public string MessageType { get; set; } = string.Empty;
}

