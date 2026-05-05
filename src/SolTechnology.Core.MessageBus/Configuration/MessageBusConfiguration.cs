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

        /// <summary>Optional retry options applied to the underlying <c>ServiceBusClient</c>.</summary>
        public ServiceBusRetryOptions? RetryOptions { get; set; }

        public List<QueueConfiguration> Queues { get; set; } = new();
    }
}

