﻿using Azure.Messaging.ServiceBus;

namespace SolTechnology.Core.MessageBus.Configuration
{
    public class MessageBusConfiguration
    {
        /// <summary>Service Bus connection string. Provide this OR rely on Managed Identity (future).</summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// When <c>true</c>, missing queues / topics / subscriptions are created at startup using
        /// <c>ServiceBusAdministrationClient</c>. Requires the <i>Manage</i> claim — not recommended
        /// in production. Defaults to <c>false</c> (provision via IaC instead).
        /// </summary>
        public bool CreateResources { get; set; }

        /// <summary>Transport — defaults to <see cref="ServiceBusTransportType.AmqpWebSockets"/> for proxy/firewall friendliness.</summary>
        public ServiceBusTransportType TransportType { get; set; } = ServiceBusTransportType.AmqpWebSockets;

        /// <summary>How many messages to process concurrently per processor. Default 1.</summary>
        public int MaxConcurrentCalls { get; set; } = 1;

        /// <summary>Number of messages prefetched for each processor. Default 0 (off).</summary>
        public int PrefetchCount { get; set; }

        /// <summary>
        /// Delivery attempt on which the receiver dead-letters a message whose handler failed, instead of abandoning it.
        /// Set it to the queue's or subscription's configured <c>MaxDeliveryCount</c>; reading it from the entity would
        /// need the <i>Manage</i> claim. Must be greater than zero. Default 10, the Service Bus default.
        /// </summary>
        public int MaxDeliveryCount { get; set; } = 10;

        /// <summary>Optional retry options applied to the underlying <c>ServiceBusClient</c>.</summary>
        public ServiceBusRetryOptions? RetryOptions { get; set; }

        public List<QueueConfiguration> Queues { get; set; } = new();
    }
}

