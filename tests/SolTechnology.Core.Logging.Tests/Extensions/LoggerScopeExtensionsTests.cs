using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace SolTechnology.Core.Logging.Tests.Extensions;

[TestFixture]
public class LoggerScopeExtensionsTests
{
    [Test]
    public void PushToScope_SingleKeyValue_ReturnsDisposable()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        logger.BeginScope(Arg.Any<Dictionary<string, object?>>())
            .Returns(Substitute.For<IDisposable>());

        // Act
        var scope = logger.PushToScope("Key", "Value");

        // Assert
        scope.Should().NotBeNull();
        logger.Received(1).BeginScope(Arg.Is<Dictionary<string, object?>>(d =>
            d.ContainsKey("Key") && (string)d["Key"]! == "Value"));
    }

    [Test]
    public void PushToScope_MultipleTuples_AddsAllToScope()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        logger.BeginScope(Arg.Any<Dictionary<string, object?>>())
            .Returns(Substitute.For<IDisposable>());

        // Act
        var scope = logger.PushToScope(("A", "1"), ("B", "2"));

        // Assert
        scope.Should().NotBeNull();
        logger.Received(1).BeginScope(Arg.Is<Dictionary<string, object?>>(d =>
            d.Count == 2 && (string)d["A"]! == "1" && (string)d["B"]! == "2"));
    }
}

