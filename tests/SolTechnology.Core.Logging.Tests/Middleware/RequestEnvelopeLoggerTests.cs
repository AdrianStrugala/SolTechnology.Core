using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Logging.Middleware;

namespace SolTechnology.Core.Logging.Tests.Middleware;

public sealed class RequestEnvelopeLoggerTests
{
    [Test]
    public void RequestFinished_LevelDisabled_DoesNotReadTimings()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(false);
        var timingService = Substitute.For<ITimingService>();
        var context = new DefaultHttpContext();

        // Act
        RequestEnvelopeLogger.RequestFinished(logger, context, timingService, elapsedMs: 5);

        // Assert
        timingService.DidNotReceive().GetTimings();
    }
}
