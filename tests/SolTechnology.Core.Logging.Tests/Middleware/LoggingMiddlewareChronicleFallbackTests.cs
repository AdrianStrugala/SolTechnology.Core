using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.Logging.Tests.Middleware;

public class LoggingMiddlewareChronicleFallbackTests : LoggingMiddlewareChronicleTestBase
{
    [Test]
    public async Task InvokeAsync_ChronicleDisabled_LogsPreviousFinishEntryWithoutTimings()
    {
        // Arrange
        BuildChronicle(o => o.Enabled = false);

        // Act
        await Invoke(_ =>
        {
            using (Chronicle.Step("Db"))
            {
                return Task.CompletedTask;
            }
        });

        // Assert
        var entry = FinishEntries.Should().ContainSingle().Which;
        entry.Id.Id.Should().Be(2146);
        entry.Message.Should().StartWith("Finished request [GET] [/api/trips] -> [200] in [").And.NotContain("Chronicle").And.NotContain("timings");
    }

    [TestCase(false, 2146, LogLevel.Information)]
    [TestCase(true, 2145, LogLevel.Error)]
    public async Task InvokeAsync_ReplacedChronicle_LogsPreviousFinishEntry(bool handlerThrows, int expectedEventId, LogLevel expectedLevel)
    {
        // Arrange
        var replacement = Substitute.For<IChronicle>();

        // Act
        await Invoke(_ => handlerThrows ? throw new InvalidOperationException("boom") : Task.CompletedTask, chronicle: replacement);

        // Assert
        replacement.Received(1).Begin("HTTP GET");
        FinishEntries.Should().ContainSingle().Which.Should().Match<FakeLogRecord>(r =>
            r.Id.Id == expectedEventId && r.Level == expectedLevel);
    }

    [TestCase("websocket")]
    [TestCase("upgrade")]
    [TestCase("server-sent events")]
    public async Task InvokeAsync_LongLivedRequest_OpensNoRootAndLogsPreviousFinishEntry(string kind)
    {
        // Act
        await Invoke(_ =>
        {
            using (Chronicle.Step("HubCall"))
            {
                return Task.CompletedTask;
            }
        }, context =>
        {
            switch (kind)
            {
                case "websocket":
                    var webSocket = Substitute.For<IHttpWebSocketFeature>();
                    webSocket.IsWebSocketRequest.Returns(true);
                    context.Features.Set(webSocket);
                    break;
                case "upgrade":
                    var upgrade = Substitute.For<IHttpUpgradeFeature>();
                    upgrade.IsUpgradableRequest.Returns(true);
                    context.Features.Set(upgrade);
                    break;
                default:
                    context.Request.Headers.Accept = "text/event-stream";
                    break;
            }
        });

        // Assert
        FinishEntries.Should().ContainSingle().Which.Id.Id.Should().Be(2146);
        Logs.GetSnapshot().Should().NotContain(r => r.Message.Contains("HubCall"));
    }

    [Test]
    public async Task InvokeAsync_SkippedPath_EmitsNoReport()
    {
        // Act
        await Invoke(_ => Task.CompletedTask, context => context.Request.Path = "/health/ready",
            options: new LoggingOptions { SkipPaths = ["/health"] });

        // Assert
        Logs.GetSnapshot().Should().BeEmpty();
    }

    [Test]
    public async Task InvokeAsync_RequestRoot_StartsNoOperationsActivity()
    {
        // Arrange
        var started = new ConcurrentQueue<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CoreLoggingActivitySources.OperationsName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => started.Enqueue(activity.OperationName)
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        await Invoke(_ =>
        {
            using (Chronicle.Step("Db"))
            {
                return Task.CompletedTask;
            }
        });

        // Assert
        started.Should().Contain("Db").And.NotContain("HTTP GET");
        FinishEntries.Should().ContainSingle().Which.Id.Id.Should().Be(2155);
    }

    [Test]
    public void RequestChronicleReported_Template_EveryPlaceholderBracketed()
    {
        // Arrange
        var method = typeof(ChronicleLogMessages).GetMethod(
            nameof(ChronicleLogMessages.RequestChronicleReported), BindingFlags.Static | BindingFlags.NonPublic)!;

        // Act
        var template = method.GetCustomAttribute<LoggerMessageAttribute>()!.Message;

        // Assert
        Regex.Matches(template, @"\{[^}]+\}").Should().HaveCount(6)
            .And.OnlyContain(m => template[m.Index - 1] == '[' && "] ".Contains(template[m.Index + m.Length]));
    }
}
