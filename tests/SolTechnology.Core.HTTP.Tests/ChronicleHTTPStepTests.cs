using System.Net;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.HTTP.Tests;

public class ChronicleHTTPStepTests : ChronicleHTTPTestBase
{
    [Test]
    public async Task Send_InsideRoot_RecordsOneStepWithMethodClientHostAndStatus()
    {
        // Act
        var report = await SendInRoot(HttpMethod.Get);

        // Assert
        report.Should().StartWith("Chronicle [Import] [Ok]")
            .And.Contain("\n`- HTTP GET Sample [@").And.EndWith("[ok] Host=api.test StatusCode=200 Attempts=1]");
    }

    [Test]
    public async Task Send_TransientFailureThenSuccess_OneStepWithAttempts2()
    {
        // Arrange
        Upstream.Then(HttpStatusCode.ServiceUnavailable).Then(HttpStatusCode.OK);

        // Act
        var report = await SendInRoot(HttpMethod.Get);

        // Assert
        report.Should().EndWith("[ok] Host=api.test StatusCode=200 Attempts=2]");
        report.Split('\n').Should().ContainSingle(l => l.Contains("HTTP GET Sample"));
    }

    [Test]
    public async Task Send_500AfterRetries_StepFailedWithAttemptsCount()
    {
        // Arrange
        Upstream.Then(HttpStatusCode.InternalServerError);

        // Act
        var report = await SendInRoot(HttpMethod.Get);

        // Assert
        Upstream.Requests.Should().HaveCount(3);
        report.Should().EndWith("[failed] Error=[HTTP500] Host=api.test StatusCode=500 Attempts=3]");
    }

    [Test]
    public async Task Send_404_StepOkWithStatusCode404()
    {
        // Arrange
        Upstream.Then(HttpStatusCode.NotFound);

        // Act
        var report = await SendInRoot(HttpMethod.Get);

        // Assert
        report.Should().StartWith("Chronicle [Import] [Ok]").And.EndWith("[ok] Host=api.test StatusCode=404 Attempts=1]");
    }

    [Test]
    public async Task Send_CallerCancelled_StepCancelled()
    {
        // Arrange
        using var caller = new CancellationTokenSource();
        Upstream.Then(async token =>
        {
            await caller.CancelAsync();
            token.ThrowIfCancellationRequested();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Act
        var report = await SendInRoot(HttpMethod.Get, cancellationToken: caller.Token);

        // Assert
        report.Should().EndWith("[cancelled] Host=api.test Attempts=1]");
    }

    [TestCase("GET", 3)]
    [TestCase("POST", 1)]
    public async Task Send_Timeout_StepFailed(string method, int expectedAttempts)
    {
        // Arrange
        Upstream.Then(async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Act
        var report = await SendInRoot(new HttpMethod(method));

        // Assert
        Upstream.Requests.Should().HaveCount(expectedAttempts);
        report.Should().Contain($"\n`- HTTP {method} Sample [@").And.Contain("[failed] Error=[TimeoutRejectedException")
            .And.EndWith($"Host=api.test Attempts={expectedAttempts}]");
    }
}
