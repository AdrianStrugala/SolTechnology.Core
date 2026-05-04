﻿namespace SolTechnology.Core.MessageBus;

/// <summary>
/// Marker contract for messages flowing through <see cref="Publish.IMessagePublisher"/>
/// and <see cref="Receive.IMessageHandler{TMessage}"/>.
/// </summary>
/// <remarks>
/// Implementations MUST return a stable, unique value from <see cref="Id"/> per
/// message instance — typically assigned in the constructor
/// (e.g. <c>Guid.NewGuid().ToString()</c>). The value is forwarded to
/// <c>ServiceBusMessage.MessageId</c>, which drives Service Bus duplicate detection
/// (when enabled on the entity) and is the primary correlation key emitted into
/// logs and traces.
/// </remarks>
public interface IMessage
{
    /// <summary>Application-level unique identifier of this message instance.</summary>
    string Id { get; }
}

