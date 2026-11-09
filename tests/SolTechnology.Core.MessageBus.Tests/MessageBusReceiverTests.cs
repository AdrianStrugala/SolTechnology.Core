using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Tests;

[TestFixture]
public class MessageBusReceiverTests
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(90);

    private sealed record ReceiverMessage(string Id, string Text) : IMessage;

    private sealed class HandlerProbe(Func<ReceiverMessage, Task> behaviour)
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public ReceiverMessage? LastMessage { get; private set; }

        public Task Handle(ReceiverMessage message)
        {
            LastMessage = message;
            Interlocked.Increment(ref _calls);
            return behaviour(message);
        }
    }

    private sealed class RunningHost(IHost host) : IAsyncDisposable
    {
        public IServiceProvider Services => host.Services;

        public async ValueTask DisposeAsync()
        {
            using var stopTimeout = new CancellationTokenSource(Deadline);
            await host.StopAsync(stopTimeout.Token);
            await ((IAsyncDisposable)host).DisposeAsync();
        }
    }

    private sealed class ProbeHandler(HandlerProbe probe) : IMessageHandler<ReceiverMessage>
    {
        public Task Handle(ReceiverMessage message, CancellationToken cancellationToken) => probe.Handle(message);
    }

    [Test]
    public async Task Publish_ThenReceive_HandlerCalledAndMessageCompleted()
    {
        // Arrange
        const string queue = "receiver-complete";
        var probe = new HandlerProbe(_ => Task.CompletedTask);
        await using var host = await StartHost(queue, probe, maxDeliveryCount: 10, new CapturingLoggerProvider());
        await using var client = new ServiceBusClient(await ServiceBusTestEnvironment.ConnectionString());
        var message = new ReceiverMessage(Guid.NewGuid().ToString(), "hello");

        // Act
        await host.Services.GetRequiredService<IMessagePublisher>().Publish(message);

        // Assert
        (await Eventually(() => Task.FromResult(probe.Calls > 0))).Should().BeTrue("the handler should receive the message");
        (await Eventually(() => IsEmpty(client, queue, SubQueue.None))).Should().BeTrue("the message should be completed");
        probe.Calls.Should().Be(1);
        probe.LastMessage.Should().Be(message);
        (await IsEmpty(client, queue, SubQueue.DeadLetter)).Should().BeTrue();
    }

    [Test]
    public async Task Receive_HandlerThrows_AbandonedThenDeadLetteredOnLastAttempt()
    {
        // Arrange
        const string queue = "receiver-dead-letter";
        var probe = new HandlerProbe(async _ =>
        {
            await Task.Yield();
            throw new InvalidOperationException("handler failed");
        });
        await using var host = await StartHost(queue, probe, maxDeliveryCount: 2, new CapturingLoggerProvider());
        await using var client = new ServiceBusClient(await ServiceBusTestEnvironment.ConnectionString());
        var message = new ReceiverMessage(Guid.NewGuid().ToString(), "poison");

        // Act
        await host.Services.GetRequiredService<IMessagePublisher>().Publish(message);

        // Assert
        var deadLettered = await ReceiveDeadLetter(client, queue);
        deadLettered.Should().NotBeNull("the message should be dead-lettered on its second delivery");
        deadLettered!.MessageId.Should().Be(message.Id);
        deadLettered.DeadLetterReason.Should().Be("HandlerFailedMaxDelivery");
        deadLettered.DeadLetterErrorDescription.Should().Be("handler failed");
        probe.Calls.Should().Be(2);
    }

    [Test]
    public async Task Receive_HandlerThrowsSync_IsNotWrapped()
    {
        // Arrange
        const string queue = "receiver-sync-throw";
        var logs = new CapturingLoggerProvider();
        var probe = new HandlerProbe(_ => throw new InvalidOperationException("sync failure"));
        await using var host = await StartHost(queue, probe, maxDeliveryCount: 1, logs);
        await using var client = new ServiceBusClient(await ServiceBusTestEnvironment.ConnectionString());
        var message = new ReceiverMessage(Guid.NewGuid().ToString(), "sync");

        // Act
        await host.Services.GetRequiredService<IMessagePublisher>().Publish(message);

        // Assert
        var deadLettered = await ReceiveDeadLetter(client, queue);
        deadLettered.Should().NotBeNull("the message should be dead-lettered on its first delivery");
        deadLettered!.DeadLetterErrorDescription.Should().Be("sync failure");
        logs.Entries.Should().Contain(entry =>
            entry.Category == typeof(MessageBusReceiver).FullName &&
            entry.Level == LogLevel.Error &&
            entry.Exception is InvalidOperationException &&
            entry.Exception.Message == "sync failure");
    }

    private static async Task<RunningHost> StartHost(
        string queue,
        HandlerProbe probe,
        int maxDeliveryCount,
        CapturingLoggerProvider logs)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Logging.AddProvider(logs);
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
        return new RunningHost(host);
    }

    private static async Task<ServiceBusReceivedMessage?> ReceiveDeadLetter(ServiceBusClient client, string queue)
    {
        await using var receiver = client.CreateReceiver(queue, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
        var deadLettered = await receiver.ReceiveMessageAsync(Deadline);
        if (deadLettered is not null)
        {
            await receiver.CompleteMessageAsync(deadLettered);
        }

        return deadLettered;
    }

    private static async Task<bool> IsEmpty(ServiceBusClient client, string queue, SubQueue subQueue)
    {
        await using var receiver = client.CreateReceiver(queue, new ServiceBusReceiverOptions { SubQueue = subQueue });
        return await receiver.PeekMessageAsync(fromSequenceNumber: 0) is null;
    }

    private static async Task<bool> Eventually(Func<Task<bool>> condition)
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
}
