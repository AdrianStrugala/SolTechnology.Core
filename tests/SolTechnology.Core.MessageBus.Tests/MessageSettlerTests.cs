using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Tests;

[TestFixture]
public class MessageSettlerTests
{
    private const int MaxDeliveryCount = 2;

    private sealed record SettledMessage(string Id) : IMessage;

    [TestCase("complete", 1, false, "Handled message [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}])")]
    [TestCase("empty body", 1, false, "Dead-lettering empty message [{MessageId}] for [{MessageType}]")]
    [TestCase("invalid JSON", 1, false, "Dead-lettering message [{MessageId}] — deserialisation to [{MessageType}] failed")]
    [TestCase("null payload", 1, false, "Dead-lettering message [{MessageId}] — null payload after deserialisation to [{MessageType}]")]
    [TestCase("handler failed", 1, false, "Handler failure for [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}]) — abandoning for retry")]
    [TestCase("handler failed", MaxDeliveryCount, false, "Handler failure for [{MessageType}] [{MessageId}] (delivery [{DeliveryCount}]) — dead-lettering on the last attempt")]
    [TestCase("complete", 1, true, "Failed to complete message [{MessageId}]; lock will expire naturally")]
    [TestCase("handler failed", 1, true, "Failed to abandon message [{MessageId}]; lock will expire naturally")]
    [TestCase("handler failed", MaxDeliveryCount, true, "Failed to dead-letter message [{MessageId}]; lock will expire naturally")]
    public async Task Settle_EveryOutcome_LogsBracketedTemplate(
        string outcome,
        int deliveryCount,
        bool settlementFails,
        string expectedTemplate)
    {
        // Arrange
        var logs = new CapturingLoggerProvider();
        var sut = new MessageSettler(logs.CreateLogger(nameof(MessageSettlerTests)), MaxDeliveryCount);
        var args = new ProcessMessageEventArgs(
            ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: "message-1", deliveryCount: deliveryCount),
            ReceiverThatFailsSettlement(settlementFails),
            CancellationToken.None);

        // Act
        await Settle(sut, args, outcome);

        // Assert
        logs.Entries.Select(entry => entry.Template).Should().Contain(expectedTemplate);
    }

    private static Task Settle(MessageSettler sut, ProcessMessageEventArgs args, string outcome)
    {
        return outcome switch
        {
            "complete" => sut.Complete(args, typeof(SettledMessage)),
            "empty body" => Unreadable(DeserializationFailure.EmptyBody, null),
            "invalid JSON" => Unreadable(DeserializationFailure.InvalidJson, new JsonReaderException("bad json")),
            "null payload" => Unreadable(DeserializationFailure.NullPayload, null),
            _ => sut.SettleFailure(args, typeof(SettledMessage), new InvalidOperationException("handler failed"))
        };

        Task Unreadable(DeserializationFailure failure, JsonException? error) =>
            sut.DeadLetterUnreadable(args, typeof(SettledMessage), new DeserializedMessage(null, failure, error));
    }

    private static ServiceBusReceiver ReceiverThatFailsSettlement(bool settlementFails)
    {
        var receiver = Substitute.For<ServiceBusReceiver>();
        if (settlementFails)
        {
            var failure = new ServiceBusException("lock lost", ServiceBusFailureReason.MessageLockLost);
            receiver.CompleteMessageAsync(Arg.Any<ServiceBusReceivedMessage>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(failure);
            receiver.AbandonMessageAsync(Arg.Any<ServiceBusReceivedMessage>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(failure);
            receiver.DeadLetterMessageAsync(Arg.Any<ServiceBusReceivedMessage>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(failure);
        }

        return receiver;
    }
}
