using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.MessageBus.Configuration;

namespace SolTechnology.Core.MessageBus.Broker;

/// <summary>
/// Creates and owns the senders and processors of the registered endpoints. All entities are provisioned before any
/// sender or processor exists, and the endpoints are published only when every one was created, so a failed
/// initialisation can be retried without duplicate senders or processors.
/// </summary>
internal sealed class ServiceBusEndpoints(
    ServiceBusClient serviceBusClient,
    MessageBusConfiguration configuration,
    ServiceBusResourceProvisioner provisioner,
    ILogger logger) : IAsyncDisposable
{
    private Dictionary<Type, List<ServiceBusSender>> _sendersByType = new();
    private Dictionary<Type, ServiceBusProcessor> _processorsByType = new();

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

    public async Task MaterializeAsync(IReadOnlyList<EndpointRegistration> endpoints, CancellationToken ct)
    {
        foreach (var endpoint in endpoints)
        {
            await provisioner.Ensure(endpoint, ct).ConfigureAwait(false);
        }

        var senders = new Dictionary<Type, List<ServiceBusSender>>();
        var processors = new Dictionary<Type, ServiceBusProcessor>();
        try
        {
            foreach (var endpoint in endpoints)
            {
                Create(endpoint, senders, processors);
            }
        }
        catch
        {
            await DisposeAll(senders, processors).ConfigureAwait(false);
            throw;
        }

        _sendersByType = senders;
        _processorsByType = processors;
    }

    private void Create(
        EndpointRegistration endpoint,
        Dictionary<Type, List<ServiceBusSender>> senders,
        Dictionary<Type, ServiceBusProcessor> processors)
    {
        if (endpoint.Role == EndpointRole.Publisher)
        {
            if (!senders.TryGetValue(endpoint.MessageType, out var typeSenders))
            {
                typeSenders = senders[endpoint.MessageType] = new List<ServiceBusSender>();
            }

            typeSenders.Add(serviceBusClient.CreateSender(endpoint.EntityName));
            return;
        }

        if (processors.ContainsKey(endpoint.MessageType))
        {
            throw new InvalidOperationException(
                $"A receiver for message type [{endpoint.MessageType.FullName}] is already registered.");
        }

        processors[endpoint.MessageType] = endpoint.Kind == EndpointKind.Queue
            ? serviceBusClient.CreateProcessor(endpoint.EntityName, BuildProcessorOptions())
            : serviceBusClient.CreateProcessor(endpoint.EntityName, endpoint.SubscriptionName!, BuildProcessorOptions());
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
        await DisposeAll(_sendersByType, _processorsByType).ConfigureAwait(false);
        _sendersByType = new();
        _processorsByType = new();
    }

    private async Task DisposeAll(
        Dictionary<Type, List<ServiceBusSender>> senders,
        Dictionary<Type, ServiceBusProcessor> processors)
    {
        foreach (var sender in senders.Values.SelectMany(typeSenders => typeSenders))
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

        foreach (var processor in processors.Values)
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
    }
}
