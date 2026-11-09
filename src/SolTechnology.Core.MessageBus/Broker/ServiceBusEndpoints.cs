using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.Broker;

/// <summary>
/// Creates and owns the senders and processors of the registered endpoints, provisioning their entities first.
/// </summary>
internal sealed class ServiceBusEndpoints(
    ServiceBusClient serviceBusClient,
    MessageBusConfiguration configuration,
    ServiceBusResourceProvisioner provisioner,
    ILogger logger) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Type, List<ServiceBusSender>> _sendersByType = new();
    private readonly ConcurrentDictionary<Type, ServiceBusProcessor> _processorsByType = new();

    public IReadOnlyList<(Type MessageType, ServiceBusProcessor Processor)> Receivers =>
        _processorsByType.Select(kv => (kv.Key, kv.Value)).ToArray();

    public IReadOnlyList<ServiceBusSender> ResolveSenders(Type messageType)
    {
        if (!_sendersByType.TryGetValue(messageType, out var senders) || senders.Count == 0)
        {
            throw new InvalidOperationException(
                $"No message-bus publisher is configured for message type [{messageType.FullName}].");
        }

        return senders;
    }

    public async Task MaterializeAsync(EndpointRegistration endpoint, CancellationToken ct)
    {
        switch (endpoint.Role, endpoint.Kind)
        {
            case (EndpointRole.Publisher, EndpointKind.Queue):
                await provisioner.EnsureQueue(endpoint.EntityName, ct).ConfigureAwait(false);
                AddSender(endpoint.MessageType, serviceBusClient.CreateSender(endpoint.EntityName));
                break;

            case (EndpointRole.Publisher, EndpointKind.Topic):
                await provisioner.EnsureTopic(endpoint.EntityName, ct).ConfigureAwait(false);
                AddSender(endpoint.MessageType, serviceBusClient.CreateSender(endpoint.EntityName));
                break;

            case (EndpointRole.Receiver, EndpointKind.Queue):
                await provisioner.EnsureQueue(endpoint.EntityName, ct).ConfigureAwait(false);
                await AddProcessorAsync(endpoint.MessageType,
                    serviceBusClient.CreateProcessor(endpoint.EntityName, BuildProcessorOptions())).ConfigureAwait(false);
                break;

            case (EndpointRole.Receiver, EndpointKind.Topic):
                if (string.IsNullOrWhiteSpace(endpoint.SubscriptionName))
                {
                    throw new ArgumentException(
                        $"Topic receiver for [{endpoint.MessageType.FullName}] requires a subscription name.");
                }
                await provisioner.EnsureTopic(endpoint.EntityName, ct).ConfigureAwait(false);
                await provisioner.EnsureSubscription(endpoint.EntityName, endpoint.SubscriptionName, ct).ConfigureAwait(false);
                await AddProcessorAsync(endpoint.MessageType, serviceBusClient.CreateProcessor(
                    endpoint.EntityName, endpoint.SubscriptionName, BuildProcessorOptions())).ConfigureAwait(false);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported endpoint combination: {endpoint.Role}/{endpoint.Kind}.");
        }
    }

    private void AddSender(Type messageType, ServiceBusSender sender) =>
        _sendersByType.AddOrUpdate(
            messageType,
            _ => new List<ServiceBusSender> { sender },
            (_, list) =>
            {
                list.Add(sender);
                return list;
            });

    private async Task AddProcessorAsync(Type messageType, ServiceBusProcessor processor)
    {
        if (!_processorsByType.TryAdd(messageType, processor))
        {
            await processor.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException(
                $"A receiver for message type [{messageType.FullName}] is already registered.");
        }
    }

    // Explicit complete / dead-letter is performed in the receiver hosted service.
    private ServiceBusProcessorOptions BuildProcessorOptions() => new()
    {
        AutoCompleteMessages = false,
        ReceiveMode = ServiceBusReceiveMode.PeekLock,
        MaxConcurrentCalls = Math.Max(1, configuration.MaxConcurrentCalls),
        PrefetchCount = Math.Max(0, configuration.PrefetchCount)
    };

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _sendersByType.Values.SelectMany(senders => senders))
        {
            try
            {
                await sender.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to dispose Service Bus sender for [{Entity}] on shutdown", sender.EntityPath);
            }
        }
        _sendersByType.Clear();

        foreach (var processor in _processorsByType.Values)
        {
            try
            {
                await processor.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to dispose Service Bus processor for [{Entity}] on shutdown", processor.EntityPath);
            }
        }
        _processorsByType.Clear();
    }
}
