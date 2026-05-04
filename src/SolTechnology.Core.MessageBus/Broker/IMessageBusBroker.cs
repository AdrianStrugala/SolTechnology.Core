﻿using Azure.Messaging.ServiceBus;

namespace SolTechnology.Core.MessageBus.Broker;

/// <summary>
/// Materialises and resolves Service Bus senders / processors based on the
/// metadata collected by <c>WithQueue*/WithTopic*</c> extension methods at
/// DI-time. All actual Service Bus client interaction (and optional resource
/// provisioning) happens asynchronously inside <see cref="EnsureInitializedAsync"/>.
/// </summary>
public interface IMessageBusBroker
{
    /// <summary>
    /// Idempotently materialises every registered endpoint (creates senders,
    /// processors and — when <see cref="Configuration.MessageBusConfiguration.CreateResources"/>
    /// is enabled — the underlying Service Bus entities). Safe to call from
    /// multiple call sites concurrently; only the first call performs work.
    /// </summary>
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>Resolves senders registered for a given message <paramref name="messageType"/>.</summary>
    IReadOnlyList<ServiceBusSender> ResolveSenders(Type messageType);

    /// <summary>Returns every (messageType, processor) pair registered as receiver.</summary>
    IReadOnlyList<(Type MessageType, ServiceBusProcessor Processor)> ResolveReceivers();
}

