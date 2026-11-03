using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.AUID.Tests;

[TestFixture]
public class AuidJsonConverterTests
{
    [Test]
    public void Read_Invalid_ThrowsJsonExceptionWithPath()
    {
        // Arrange
        const string json = """{"Id":"not-an-auid"}""";

        // Act
        var act = () => JsonSerializer.Deserialize<AuidHolder>(json);

        // Assert
        act.Should().Throw<JsonException>()
            .Where(exception => exception.Path == "$.Id" && exception.Message.Contains("not-an-auid"));
    }

    [Test]
    public void Read_Empty_ThrowsJsonException()
    {
        // Arrange
        const string json = """{"Id":""}""";

        // Act
        var act = () => JsonSerializer.Deserialize<AuidHolder>(json);

        // Assert
        act.Should().Throw<JsonException>().Where(exception => exception.Path == "$.Id");
    }

    [Test]
    public void RoundTrip_DictionaryKey()
    {
        // Arrange
        var first = Auid.Parse("ORD_20241205123456_012345");
        var second = Auid.Parse("USR_20241205123457_000001");
        var original = new Dictionary<Auid, int> { [first] = 1, [second] = 2 };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Dictionary<Auid, int>>(json);

        // Assert
        json.Should().Be("""{"ORD_20241205123456_012345":1,"USR_20241205123457_000001":2}""");
        deserialized.Should().BeEquivalentTo(original);
    }

    public record AuidHolder(Auid Id);
}
