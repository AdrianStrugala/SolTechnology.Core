using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;
using static SolTechnology.Core.MessageBus.Tests.ReceiverTestHost;

namespace SolTechnology.Core.MessageBus.Tests;

[TestFixture]
public class MessageBusReceiverSettlementTests
{
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
    public async Task Stop_HandlerFinishesAfterShutdownStarts_MessageCompletedNotRedelivered()
    {
        // Arrange
        const string queue = "receiver-shutdown";
        var logs = new CapturingLoggerProvider();
        var shutdownSignalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new HandlerProbe(async (_, cancellationToken) =>
        {
            cancellationToken.Register(() => shutdownSignalled.TrySetResult());
            await release.Task;
        });
        await using var host = await StartHost(queue, probe, maxDeliveryCount: 10, logs);
        await using var client = new ServiceBusClient(await ServiceBusTestEnvironment.ConnectionString());
        var receiver = host.Services.GetServices<IHostedService>().OfType<MessageBusReceiver>().Single();
        await host.Services.GetRequiredService<IMessagePublisher>().Publish(new ReceiverMessage(Guid.NewGuid().ToString(), "deploy"));
        (await Eventually(() => Task.FromResult(probe.Calls > 0))).Should().BeTrue("the handler should receive the message");

        // Act
        var stop = receiver.StopAsync(CancellationToken.None);
        await shutdownSignalled.Task.WaitAsync(Deadline);
        release.SetResult();
        await stop.WaitAsync(Deadline);

        // Assert
        (await IsEmpty(client, queue, SubQueue.None)).Should().BeTrue("a handler that succeeded during shutdown completes its message");
        (await IsEmpty(client, queue, SubQueue.DeadLetter)).Should().BeTrue();
        probe.Calls.Should().Be(1);
        logs.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Warning && entry.Category == typeof(MessageBusReceiver).FullName);
    }
}
