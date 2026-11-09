using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Tests;

/// <summary>
/// A started host with one queue publisher and one queue receiver against the Service Bus emulator, plus the
/// polling helpers the receiver tests assert with.
/// </summary>
internal sealed class ReceiverTestHost(IHost host) : IAsyncDisposable
{
    public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(90);

    public sealed record ReceiverMessage(string Id, string Text) : IMessage;

    public sealed class HandlerProbe(Func<ReceiverMessage, CancellationToken, Task> behaviour)
    {
        private int _calls;

        public HandlerProbe(Func<ReceiverMessage, Task> behaviour) : this((message, _) => behaviour(message))
        {
        }

        public int Calls => Volatile.Read(ref _calls);

        public ReceiverMessage? LastMessage { get; private set; }

        public Task Handle(ReceiverMessage message, CancellationToken cancellationToken)
        {
            LastMessage = message;
            Interlocked.Increment(ref _calls);
            return behaviour(message, cancellationToken);
        }
    }

    public sealed class ProbeHandler(HandlerProbe probe) : IMessageHandler<ReceiverMessage>
    {
        public Task Handle(ReceiverMessage message, CancellationToken cancellationToken) => probe.Handle(message, cancellationToken);
    }

    public IServiceProvider Services => host.Services;

    public static async Task<ReceiverTestHost> StartHost(
        string queue,
        HandlerProbe probe,
        int maxDeliveryCount,
        CapturingLoggerProvider logs)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Logging.AddProvider(logs).SetMinimumLevel(LogLevel.Trace);
        builder.Services.AddSingleton(probe);
        builder.Services
            .AddSolMessageBus(new MessageBusConfiguration
            {
                ConnectionString = await ServiceBusTestEnvironment.ConnectionString(),
                TransportType = ServiceBusTransportType.AmqpTcp,
                MaxDeliveryCount = maxDeliveryCount
            })
            .WithQueuePublisher<ReceiverMessage>(queue)
            .WithQueueReceiver<ReceiverMessage, ProbeHandler>(queue);

        var host = builder.Build();
        using var startTimeout = new CancellationTokenSource(Deadline);
        await host.StartAsync(startTimeout.Token);
        return new ReceiverTestHost(host);
    }

    public static async Task<ServiceBusReceivedMessage?> ReceiveDeadLetter(ServiceBusClient client, string queue)
    {
        await using var receiver = client.CreateReceiver(queue, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
        var deadLettered = await receiver.ReceiveMessageAsync(Deadline);
        if (deadLettered is not null)
        {
            await receiver.CompleteMessageAsync(deadLettered);
        }

        return deadLettered;
    }

    public static async Task<bool> IsEmpty(ServiceBusClient client, string queue, SubQueue subQueue)
    {
        await using var receiver = client.CreateReceiver(queue, new ServiceBusReceiverOptions { SubQueue = subQueue });
        return await receiver.PeekMessageAsync(fromSequenceNumber: 0) is null;
    }

    public static async Task<bool> Eventually(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow + Deadline;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        using var stopTimeout = new CancellationTokenSource(Deadline);
        await host.StopAsync(stopTimeout.Token);
        await ((IAsyncDisposable)host).DisposeAsync();
    }
}
