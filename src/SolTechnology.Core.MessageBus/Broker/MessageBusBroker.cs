﻿using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Options;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.Broker
{
    /// <summary>
    /// Default <see cref="IMessageBusBroker"/>. Owns one <see cref="ServiceBusClient"/>
    /// per process, materialises senders/processors lazily on first
    /// <see cref="EnsureInitializedAsync"/> call and disposes them deterministically.
    /// </summary>
    public sealed class MessageBusBroker : IMessageBusBroker, IAsyncDisposable
    {
        private readonly MessageBusConfiguration _configuration;
        private readonly MessageBusRegistry _registry;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusAdministrationClient? _managementClient;

        private readonly ConcurrentDictionary<Type, List<ServiceBusSender>> _sendersByType = new();
        private readonly ConcurrentDictionary<Type, ServiceBusProcessor> _processorsByType = new();

        private readonly SemaphoreSlim _initLock = new(1, 1);
        private volatile bool _initialized;
        private volatile bool _disposed;

        internal MessageBusBroker(
            IOptions<MessageBusConfiguration> options,
            MessageBusRegistry registry)
        {
            _configuration = options.Value ?? throw new ArgumentNullException(nameof(options));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));

            if (string.IsNullOrWhiteSpace(_configuration.ConnectionString))
            {
                throw new ArgumentException(
                    $"{nameof(MessageBusConfiguration)}.{nameof(MessageBusConfiguration.ConnectionString)} is required.",
                    nameof(options));
            }

            var clientOptions = new ServiceBusClientOptions
            {
                TransportType = _configuration.TransportType
            };
            if (_configuration.RetryOptions is not null)
            {
                clientOptions.RetryOptions = _configuration.RetryOptions;
            }

            _serviceBusClient = new ServiceBusClient(_configuration.ConnectionString, clientOptions);

            if (_configuration.CreateResources)
            {
                _managementClient = new ServiceBusAdministrationClient(_configuration.ConnectionString);
            }
        }

        public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized) return;
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_initialized) return;

                foreach (var endpoint in _registry.Endpoints)
                {
                    await MaterializeAsync(endpoint, cancellationToken).ConfigureAwait(false);
                }

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public IReadOnlyList<ServiceBusSender> ResolveSenders(Type messageType)
        {
            ArgumentNullException.ThrowIfNull(messageType);
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    $"{nameof(MessageBusBroker)} is not initialised. Call {nameof(EnsureInitializedAsync)} first.");
            }

            if (!_sendersByType.TryGetValue(messageType, out var senders) || senders.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No message-bus publisher is configured for message type [{messageType.FullName}].");
            }

            return senders;
        }

        public IReadOnlyList<(Type MessageType, ServiceBusProcessor Processor)> ResolveReceivers()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    $"{nameof(MessageBusBroker)} is not initialised. Call {nameof(EnsureInitializedAsync)} first.");
            }

            return _processorsByType
                .Select(kv => (kv.Key, kv.Value))
                .ToArray();
        }

        private async Task MaterializeAsync(EndpointRegistration endpoint, CancellationToken ct)
        {
            switch (endpoint.Role, endpoint.Kind)
            {
                case (EndpointRole.Publisher, EndpointKind.Queue):
                    await EnsureQueueAsync(endpoint.EntityName, ct).ConfigureAwait(false);
                    AddSender(endpoint.MessageType, _serviceBusClient.CreateSender(endpoint.EntityName));
                    break;

                case (EndpointRole.Publisher, EndpointKind.Topic):
                    await EnsureTopicAsync(endpoint.EntityName, ct).ConfigureAwait(false);
                    AddSender(endpoint.MessageType, _serviceBusClient.CreateSender(endpoint.EntityName));
                    break;

                case (EndpointRole.Receiver, EndpointKind.Queue):
                    await EnsureQueueAsync(endpoint.EntityName, ct).ConfigureAwait(false);
                    var queueProcessor = _serviceBusClient.CreateProcessor(endpoint.EntityName, BuildProcessorOptions());
                    if (!_processorsByType.TryAdd(endpoint.MessageType, queueProcessor))
                    {
                        await queueProcessor.DisposeAsync().ConfigureAwait(false);
                        throw new InvalidOperationException(
                            $"A receiver for message type [{endpoint.MessageType.FullName}] is already registered.");
                    }
                    break;

                case (EndpointRole.Receiver, EndpointKind.Topic):
                    if (string.IsNullOrWhiteSpace(endpoint.SubscriptionName))
                    {
                        throw new ArgumentException(
                            $"Topic receiver for [{endpoint.MessageType.FullName}] requires a subscription name.");
                    }
                    await EnsureTopicAsync(endpoint.EntityName, ct).ConfigureAwait(false);
                    await EnsureSubscriptionAsync(endpoint.EntityName, endpoint.SubscriptionName, ct).ConfigureAwait(false);
                    var topicProcessor = _serviceBusClient.CreateProcessor(
                        endpoint.EntityName,
                        endpoint.SubscriptionName,
                        BuildProcessorOptions());
                    if (!_processorsByType.TryAdd(endpoint.MessageType, topicProcessor))
                    {
                        await topicProcessor.DisposeAsync().ConfigureAwait(false);
                        throw new InvalidOperationException(
                            $"A receiver for message type [{endpoint.MessageType.FullName}] is already registered.");
                    }
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported endpoint combination: {endpoint.Role}/{endpoint.Kind}.");
            }
        }

        private void AddSender(Type messageType, ServiceBusSender sender)
        {
            _sendersByType.AddOrUpdate(
                messageType,
                _ => new List<ServiceBusSender> { sender },
                (_, list) =>
                {
                    list.Add(sender);
                    return list;
                });
        }

        private ServiceBusProcessorOptions BuildProcessorOptions() => new()
        {
            // Explicit complete / dead-letter is performed in the receiver hosted service.
            AutoCompleteMessages = false,
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxConcurrentCalls = Math.Max(1, _configuration.MaxConcurrentCalls),
            PrefetchCount = Math.Max(0, _configuration.PrefetchCount)
        };

        private async Task EnsureQueueAsync(string queueName, CancellationToken ct)
        {
            if (_managementClient is null) return;
            if (!await _managementClient.QueueExistsAsync(queueName, ct).ConfigureAwait(false))
            {
                await _managementClient.CreateQueueAsync(queueName, ct).ConfigureAwait(false);
            }
        }

        private async Task EnsureTopicAsync(string topicName, CancellationToken ct)
        {
            if (_managementClient is null) return;
            if (!await _managementClient.TopicExistsAsync(topicName, ct).ConfigureAwait(false))
            {
                await _managementClient.CreateTopicAsync(topicName, ct).ConfigureAwait(false);
            }
        }

        private async Task EnsureSubscriptionAsync(string topicName, string subscriptionName, CancellationToken ct)
        {
            if (_managementClient is null) return;
            if (!await _managementClient.SubscriptionExistsAsync(topicName, subscriptionName, ct).ConfigureAwait(false))
            {
                await _managementClient.CreateSubscriptionAsync(topicName, subscriptionName, ct).ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var senderList in _sendersByType.Values)
            {
                foreach (var sender in senderList)
                {
                    try { await sender.DisposeAsync().ConfigureAwait(false); } catch { /* swallow on shutdown */ }
                }
            }
            _sendersByType.Clear();

            foreach (var processor in _processorsByType.Values)
            {
                try { await processor.DisposeAsync().ConfigureAwait(false); } catch { /* swallow on shutdown */ }
            }
            _processorsByType.Clear();

            await _serviceBusClient.DisposeAsync().ConfigureAwait(false);
            _initLock.Dispose();
        }
    }
}

