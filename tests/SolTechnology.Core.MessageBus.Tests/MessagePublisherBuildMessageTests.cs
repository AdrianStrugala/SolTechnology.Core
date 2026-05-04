using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SolTechnology.Core.MessageBus;
using SolTechnology.Core.MessageBus.Publish;

namespace SolTechnology.Core.MessageBus.Tests;

public sealed class MessagePublisherBuildMessageTests
{
    private sealed record TripBookedEvent(string Id, string Destination) : IMessage;

    [Test]
    public void BuildMessage_sets_message_id_content_type_subject_and_type_property()
    {
        var msg = new TripBookedEvent(Guid.NewGuid().ToString(), "Reykjavik");

        var sbm = MessagePublisher.BuildMessage(msg, msg.GetType());

        sbm.MessageId.Should().Be(msg.Id);
        sbm.ContentType.Should().Be("application/json");
        sbm.Subject.Should().Be(nameof(TripBookedEvent));
        sbm.ApplicationProperties["Type"].Should().Be(nameof(TripBookedEvent));

        var roundTripped = JsonConvert.DeserializeObject<TripBookedEvent>(
            Encoding.UTF8.GetString(sbm.Body));
        roundTripped.Should().Be(msg);
    }

    [Test]
    public void BuildMessage_falls_back_to_new_guid_when_id_is_missing()
    {
        var msg = new TripBookedEvent(string.Empty, "Reykjavik");

        var sbm = MessagePublisher.BuildMessage(msg, msg.GetType());

        sbm.MessageId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(sbm.MessageId, out _).Should().BeTrue(
            "the publisher must never emit a Service Bus message without a MessageId");
    }
}

