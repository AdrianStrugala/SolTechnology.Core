using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.MessageBus.Broker;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Tests;

[TestFixture]
public class MessageBusReceiverCompletionTests
{
    private const string ConnectionString =
        "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v";

    public sealed record CompletedMessage(string Id) : IMessage;

    public sealed class SucceedingHandler : IMessageHandler<CompletedMessage>
    {
        public Task Handle(CompletedMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ManualProcessor(ServiceBusClient client) : ServiceBusProcessor(client, "orders", new ServiceBusProcessorOptions())
    {
        public override Task StartProcessingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Deliver(ProcessMessageEventArgs args) => OnProcessMessageAsync(args);
    }

    private sealed class SingleReceiverBroker(ServiceBusProcessor processor) : IMessageBusBroker
    {
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public IReadOnlyList<ServiceBusSender> ResolveSenders(Type messageType) => [];

        public IReadOnlyList<(Type MessageType, ServiceBusProcessor Processor)> ResolveReceivers() =>
            [(typeof(CompletedMessage), processor)];
    }

    [Test]
    public async Task HandleMessage_HandlerSucceedsButCompleteFails_LogsWarningAndDoesNotSettleAsFailure()
    {
        // Arrange
        var logs = new CapturingLoggerProvider();
        await using var client = new ServiceBusClient(ConnectionString);
        await using var processor = new ManualProcessor(client);
        var sut = CreateReceiver(processor, logs, maxDeliveryCount: 1);
        await sut.StartAsync(CancellationToken.None);
        var receiver = Substitute.For<ServiceBusReceiver>();
        receiver.CompleteMessageAsync(Arg.Any<ServiceBusReceivedMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ServiceBusException("lock lost", ServiceBusFailureReason.MessageLockLost));
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{\"Id\":\"message-1\"}"), messageId: "message-1", deliveryCount: 1);

        // Act
        await processor.Deliver(new ProcessMessageEventArgs(message, receiver, CancellationToken.None));

        // Assert
        logs.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Template == "Failed to complete message [{MessageId}]; lock will expire naturally" &&
            entry.Exception is ServiceBusException);
        logs.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Error);
        await receiver.DidNotReceiveWithAnyArgs().DeadLetterMessageAsync(default!, default(string)!, default!, default);
        await receiver.DidNotReceiveWithAnyArgs().AbandonMessageAsync(default!, default, default);
    }

    private static MessageBusReceiver CreateReceiver(ServiceBusProcessor processor, CapturingLoggerProvider logs, int maxDeliveryCount)
    {
        var services = new ServiceCollection()
            .AddScoped<IMessageHandler<CompletedMessage>, SucceedingHandler>()
            .BuildServiceProvider();
        var options = Options.Create(new MessageBusConfiguration { ConnectionString = ConnectionString, MaxDeliveryCount = maxDeliveryCount });
        var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs).SetMinimumLevel(LogLevel.Trace));
        return new MessageBusReceiver(
            new SingleReceiverBroker(processor),
            services,
            Substitute.For<ICorrelationIdService>(),
            options,
            loggerFactory.CreateLogger<MessageBusReceiver>());
    }
}
