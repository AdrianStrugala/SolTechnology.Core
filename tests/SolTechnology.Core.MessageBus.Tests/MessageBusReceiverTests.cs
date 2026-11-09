using System.Text.RegularExpressions;
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

    [Test]
    public async Task PublishReceiveAndStop_LogTemplates_BracketEveryPlaceholder()
    {
        // Arrange
        const string queue = "receiver-complete";
        var logs = new CapturingLoggerProvider();
        var probe = new HandlerProbe(_ => Task.CompletedTask);
        await using var host = await StartHost(queue, probe, maxDeliveryCount: 10, logs);
        var receiver = host.Services.GetServices<IHostedService>().OfType<MessageBusReceiver>().Single();

        // Act
        await host.Services.GetRequiredService<IMessagePublisher>().Publish(new ReceiverMessage(Guid.NewGuid().ToString(), "logs"));
        await Eventually(() => Task.FromResult(HasTemplate(logs, "Handled message [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}])")));
        await receiver.StopAsync(new CancellationToken(canceled: true));

        // Assert
        Templates(logs).Should().Contain(new[]
        {
            "Starting Service Bus processor for [{MessageType}] on [{Entity}]",
            "Published message [{MessageType}] [{MessageId}] to [{Entity}]",
            "Handled message [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}])",
            "Failed to stop Service Bus processor on [{Entity}]"
        });
        Templates(logs).Should().AllSatisfy(template => UnbracketedPlaceholder.IsMatch(template).Should().BeFalse(template));
    }

    [Test]
    public async Task MissingQueue_LogTemplates_BracketEveryPlaceholder()
    {
        // Arrange
        const string queue = "receiver-missing";
        var logs = new CapturingLoggerProvider();
        await using var host = await StartHost(queue, new HandlerProbe(_ => Task.CompletedTask), maxDeliveryCount: 10, logs);

        // Act
        var publish = () => host.Services.GetRequiredService<IMessagePublisher>()
            .Publish(new ReceiverMessage(Guid.NewGuid().ToString(), "missing"));

        // Assert
        await publish.Should().ThrowAsync<Exception>();
        (await Eventually(() => Task.FromResult(HasTemplate(logs, ProcessorErrorTemplate)))).Should().BeTrue();
        Templates(logs).Should().Contain("Failed to publish message [{MessageType}] [{MessageId}] to [{Entity}]");
        Templates(logs).Should().AllSatisfy(template => UnbracketedPlaceholder.IsMatch(template).Should().BeFalse(template));
    }

    private const string ProcessorErrorTemplate =
        "Service Bus processor error on [{Entity}] ([{Source}]/[{Namespace}]): [{Message}]";

    private static readonly Regex UnbracketedPlaceholder = new(@"(?<!\[)\{[A-Za-z]+\}|\{[A-Za-z]+\}(?!\])");

    private static IReadOnlyCollection<string> Templates(CapturingLoggerProvider logs) =>
        logs.Entries
            .Where(entry => entry.Category.StartsWith("SolTechnology.Core.MessageBus", StringComparison.Ordinal))
            .Select(entry => entry.Template)
            .OfType<string>()
            .Distinct()
            .ToArray();

    private static bool HasTemplate(CapturingLoggerProvider logs, string template) => Templates(logs).Contains(template);

    private static async Task<RunningHost> StartHost(
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
