using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Middleware;

namespace SolTechnology.Core.Logging.Tests.Middleware;

public sealed class RequestEnvelopeLoggerTests
{
    [Test]
    public void RequestEnded_RecordedRootAndLevelDisabled_WritesNothing()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(false);
        var chronicle = TestChronicles.Create();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        var root = ChronicleRequestRoot.Begin(chronicle, context);

        // Act
        RequestEnvelopeLogger.RequestEnded(logger, context, root, exception: null, clientAborted: false, elapsedMs: 5);

        // Assert
        root.IsRecording.Should().BeTrue();
        logger.ReceivedCalls().Should().OnlyContain(call => call.GetMethodInfo().Name == nameof(ILogger.IsEnabled));
    }
}
