using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Middleware;

namespace SolTechnology.Core.Logging.Tests.Middleware;

public class LoggingMiddlewareChronicleTests : LoggingMiddlewareChronicleTestBase
{
    [Test]
    public async Task InvokeAsync_SuccessfulRequest_EmitsOneEntryWithEventId2155()
    {
        // Act
        await Invoke(_ => Task.CompletedTask);

        // Assert
        var entry = FinishEntries.Should().ContainSingle().Which;
        entry.Id.Should().Be(new EventId(2155, "RequestChronicleReported"));
        entry.Level.Should().Be(LogLevel.Information);
        entry.Category.Should().Be(typeof(LoggingMiddleware).FullName);
        entry.Message.Should().StartWith("Finished request [GET] [/api/trips] -> [200] in [")
            .And.Contain(". Chronicle: [Ok] [\nHTTP GET [@0 ms] [0 ms] [ok] Route=unmatched StatusCode=200]");
    }

    [Test]
    public async Task InvokeAsync_EndpointSteps_ReportContainsNestedSteps()
    {
        // Act
        await Invoke(_ =>
        {
            using (Chronicle.Step("LoadCities"))
            {
                Time.Advance(TimeSpan.FromMilliseconds(30));
                using (Chronicle.Step("Db"))
                {
                    Time.Advance(TimeSpan.FromMilliseconds(20));
                }
            }

            return Task.CompletedTask;
        });

        // Assert
        FinishEntries.Should().ContainSingle().Which.Message.Should().EndWith(
            "\nHTTP GET [@0 ms] [50 ms] [ok] Route=unmatched StatusCode=200\n`- LoadCities [@0 ms] [50 ms] [ok]\n   `- Db [@30 ms] [20 ms] [ok]]");
    }

    [TestCase(StatusCodes.Status200OK, 2000, "Ok", false)]
    [TestCase(StatusCodes.Status404NotFound, 0, "Ok", false)]
    [TestCase(StatusCodes.Status200OK, 0, "Ok", true)]
    public async Task InvokeAsync_SlowRequest404OrFailedStep_LogsWarning(int statusCode, int durationMs, string chronicleStatus, bool failStep)
    {
        // Act
        await Invoke(context =>
        {
            using (var step = Chronicle.Step("Db"))
            {
                if (failStep)
                {
                    step.Fail("Timeout");
                }
            }

            Time.Advance(TimeSpan.FromMilliseconds(durationMs));
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });

        // Assert
        var entry = FinishEntries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain($"-> [{statusCode}]").And.Contain($"Chronicle: [{chronicleStatus}]");
    }

    [Test]
    public async Task InvokeAsync_HandlerThrows_LogsExactlyOneErrorEntryWithException()
    {
        // Act
        var context = await Invoke(_ => throw new InvalidOperationException("boom"));

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        Logs.GetSnapshot().Where(r => r.Level >= LogLevel.Error).Should().ContainSingle().Which.Should().Match<FakeLogRecord>(r =>
            r.Id.Id == 2155 && r.Exception is InvalidOperationException);
        FinishEntries.Single().Message.Should().Contain("Chronicle: [Failed]").And.Contain("Error=[InvalidOperationException: boom]");
    }

    [Test]
    public async Task InvokeAsync_HandledExceptionMappedTo500_LogsErrorWithChronicleStatusFailed()
    {
        // Act
        await Invoke(context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        });

        // Assert
        var entry = FinishEntries.Should().ContainSingle().Which;
        entry.Should().Match<FakeLogRecord>(r =>
            r.Id.Id == 2155 && r.Level == LogLevel.Error && r.Exception == null);
        entry.Message.Should().Contain("Chronicle: [Failed]").And.Contain("Error=[HTTP500]");
    }

    [Test]
    public async Task InvokeAsync_ClientAbort_LogsWarningCancelled()
    {
        // Arrange
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();

        // Act
        await Invoke(context => throw new OperationCanceledException(context.RequestAborted),
            context => context.RequestAborted = aborted.Token);

        // Assert
        var entry = FinishEntries.Should().ContainSingle().Which;
        entry.Should().Match<FakeLogRecord>(r =>
            r.Id.Id == 2155 && r.Level == LogLevel.Warning && r.Exception is OperationCanceledException);
        entry.Message.Should().Contain("Chronicle: [Cancelled]");
    }

    [TestCase(null, "unmatched")]
    [TestCase("api/cities/{name}", "api/cities/{name}")]
    public async Task InvokeAsync_RouteAttribute_UsesTemplateNotRawPath(string? template, string expectedRoute)
    {
        // Act
        await Invoke(_ => Task.CompletedTask, context =>
        {
            context.Request.Path = "/api/cities/Warsaw";
            if (template is not null)
            {
                context.SetEndpoint(new RouteEndpoint(
                    _ => Task.CompletedTask, RoutePatternFactory.Parse(template), 0, EndpointMetadataCollection.Empty, "cities"));
            }
        });

        // Assert
        var tree = FinishEntries.Single().Message.Split(". Chronicle: ")[1];
        tree.Should().Contain($" Route={expectedRoute} ").And.NotContain("Warsaw");
    }
}
