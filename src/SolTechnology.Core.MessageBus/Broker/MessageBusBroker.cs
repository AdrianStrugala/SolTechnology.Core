﻿using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.Broker
{
    /// <summary>
    /// Default <see cref="IMessageBusBroker"/>. Uses the shared <see cref="ServiceBusClient"/> that DI
    /// owns, materialises senders/processors lazily on first
    /// <see cref="EnsureInitializedAsync"/> call and disposes them deterministically.
    /// </summary>
    internal sealed class MessageBusBroker(
        IOptions<MessageBusConfiguration> options,
        MessageBusRegistry registry,
        ServiceBusClient serviceBusClient,
        ILogger<MessageBusBroker> logger,
        ServiceBusAdministrationClient? administrationClient = null) : IMessageBusBroker, IAsyncDisposable
    {
        private readonly ServiceBusEndpoints _endpoints = new(
            serviceBusClient,
            options.Value,
            new ServiceBusResourceProvisioner(
                options.Value.CreateResources
                    ? administrationClient ?? new ServiceBusAdministrationClient(options.Value.ConnectionString)
                    : null,
                options.Value.MaxDeliveryCount,
                logger),
            logger);

        private readonly SemaphoreSlim _initLock = new(1, 1);
        private volatile bool _initialized;
        private volatile bool _disposed;

        public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            ObjectDisposedException.ThrowIf(_disposed, this);

            await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_initialized)
                {
                    return;
                }

                await _endpoints.MaterializeAsync(registry.Endpoints, cancellationToken).ConfigureAwait(false);
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
            ThrowIfNotInitialized();

            return _endpoints.ResolveSenders(messageType);
        }

        public IReadOnlyList<(Type MessageType, ServiceBusProcessor Processor)> ResolveReceivers()
        {
            ThrowIfNotInitialized();

            return _endpoints.Receivers;
        }

        private void ThrowIfNotInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    $"{nameof(MessageBusBroker)} is not initialised. Call {nameof(EnsureInitializedAsync)} first.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await _endpoints.DisposeAsync().ConfigureAwait(false);
            _initLock.Dispose();
        }
    }
}
