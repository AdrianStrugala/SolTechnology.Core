using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.MessageBus;

namespace SolTechnology.Core.MessageBus.Tests;

/// <summary>
/// Regression — in 0.5.0 <c>IMessage.Id</c> was implemented via a static field on
/// the interface and returned the same GUID for every instance in the AppDomain.
/// </summary>
public sealed class IMessageIdTests
{
    private sealed record SampleMessage(string Id) : IMessage;

    [Test]
    public void Two_messages_have_distinct_ids()
    {
        IMessage a = new SampleMessage(Guid.NewGuid().ToString());
        IMessage b = new SampleMessage(Guid.NewGuid().ToString());

        a.Id.Should().NotBe(b.Id);
    }

    [Test]
    public void Implementation_supplied_id_is_preserved()
    {
        var id = Guid.NewGuid().ToString();
        IMessage msg = new SampleMessage(id);

        msg.Id.Should().Be(id);
    }
}

