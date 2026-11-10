using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Masking;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleKeyAndErrorTypeLimitsTests : ChronicleTestBase
{
    [TestCase(false)]
    [TestCase(true)]
    public void AttributeKey_TooLong_TruncatedWithMarker(bool masked)
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxAttributeValueLength = 16);
        var key = new string('k', 40);

        // Act
        using (var root = sut.Begin("Root"))
        {
            _ = masked ? root.SetMasked(key, "secret@example.com", MaskMode.Full) : root.Set(key, "value");
        }

        // Assert
        Listener.Completed.Single().Node.SnapshotAttributes().Should().ContainSingle()
            .Which.Key.Should().Be(new string('k', 16) + "[truncated]");
    }

    [Test]
    public void ErrorType_TooLong_TruncatedWithMarker()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxAttributeValueLength = 16);

        // Act
        using (var root = sut.Begin("Root"))
        {
            root.Fail(new string('e', 40), "boom");
        }

        // Assert
        var node = Listener.Completed.Single().Node;
        node.ErrorType.Should().Be(new string('e', 16) + "[truncated]");
        node.ErrorMessage.Should().Be("boom");
    }
}
