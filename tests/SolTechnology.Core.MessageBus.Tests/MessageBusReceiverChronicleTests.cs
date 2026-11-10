using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace SolTechnology.Core.MessageBus.Tests;

[TestFixture]
public class MessageBusReceiverChronicleTests : MessageBusReceiverChronicleTestBase
{
    [Test]
    public async Task HandleMessage_Completed_EmitsOneRootReportOk()
    {
        // Act
        await Handle(Body("ok"));

        // Assert
        var report = Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Information);
        report.Message.Should().StartWith("Chronicle [Message ProbeMessage] [Ok]")
            .And.Contain("MessageId=message-1-35 DeliveryCount=1 Entity=orders Settlement=Completed")
            .And.Contain("\n`- SaveOrder [@").And.NotContain("secret-body-id");
        report.Scopes.OfType<IEnumerable<KeyValuePair<string, object?>>>().SelectMany(s => s)
            .Should().Contain(p => p.Key == "MessageId" && Equals(p.Value, "message-1-35"));
    }

    [Test]
    public async Task HandleMessage_CompleteFails_RootOkWithCompleteFailed()
    {
        // Act
        await Handle(Body("ok"), completeFails: true);

        // Assert
        Reports.Should().ContainSingle().Which.Message.Should().StartWith("Chronicle [Message ProbeMessage] [Ok]")
            .And.Contain("Settlement=CompleteFailed");
    }

    [TestCase(1, "Settlement=Abandoned]")]
    [TestCase(3, "Settlement=DeadLettered DeadLetterReason=HandlerFailedMaxDelivery]")]
    public async Task HandleMessage_HandlerThrows_RootFailedWithExceptionAndSettlement(int deliveryCount, string settlement)
    {
        // Act
        await Handle(Body("throw"), deliveryCount);

        // Assert
        var report = Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Error);
        report.Exception.Should().BeOfType<InvalidOperationException>();
        report.Message.Should().StartWith("Chronicle [Message ProbeMessage] [Failed]")
            .And.Contain("Error=[InvalidOperationException: boom]").And.Contain(settlement.TrimEnd(']'));
    }

    [TestCase("", "EmptyBody")]
    [TestCase("{not json", "DeserializationFailed")]
    [TestCase("null", "NullPayload")]
    public async Task HandleMessage_UnreadableBody_RootFailedDeadLetteredWithoutDescription(string body, string reason)
    {
        // Act
        await Handle(body);

        // Assert
        Reports.Should().ContainSingle().Which.Message.Should().StartWith("Chronicle [Message ProbeMessage] [Failed]")
            .And.Contain($"Error=[DeadLettered: {reason}]").And.Contain($"Settlement=DeadLettered DeadLetterReason={reason}")
            .And.NotContain("Unexpected character").And.NotContain("Path ");
    }

    [Test]
    public async Task HandleMessage_ProcessorStopping_RootCancelled()
    {
        // Arrange
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        // Act
        var act = () => Handle(Body("cancel"), cancellationToken: stopping.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        var report = Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Warning);
        report.Message.Should().StartWith("Chronicle [Message ProbeMessage] [Cancelled]").And.NotContain("Settlement=");
    }

    [Test]
    public async Task HandleMessage_TwoConcurrentMessages_TwoIndependentRoots()
    {
        // Act
        await Task.WhenAll(Handle(Body("wait")), Handle(Body("wait"), deliveryCount: 2));

        // Assert
        Reports.Should().HaveCount(2).And.OnlyContain(r =>
            r.Message.StartsWith("Chronicle [Message ProbeMessage] [Ok]") && r.Message.Split("SaveOrder").Length == 2);
        Reports.Select(r => r.Message.Contains("message-1-") ? 1 : 2).Should().BeEquivalentTo([1, 2]);
    }
}
