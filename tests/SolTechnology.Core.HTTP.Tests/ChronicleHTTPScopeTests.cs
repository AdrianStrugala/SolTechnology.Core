using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.HTTP.Handlers;

namespace SolTechnology.Core.HTTP.Tests;

public class ChronicleHTTPScopeTests : ChronicleHTTPTestBase
{
    [Test]
    public async Task Send_PropagateCorrelationFalse_StillRecordsStep()
    {
        // Arrange
        Build(propagateCorrelation: false);

        // Act
        var report = await SendInRoot(HttpMethod.Get);

        // Assert
        report.Should().Contain("\n`- HTTP GET Sample [@").And.EndWith("StatusCode=200 Attempts=1]");
    }

    [Test]
    public async Task Send_PathAndQuery_NeverRecorded()
    {
        // Act
        var report = await SendInRoot(HttpMethod.Get, "api/cities/Warsaw?email=jan.kowalski@example.com");

        // Assert
        report.Should().NotContain("Warsaw").And.NotContain("jan.kowalski").And.NotContain("api/cities");
    }

    [Test]
    public async Task Send_OutsideRoot_RecordsNothing()
    {
        // Act
        using var response = await Client.GetAsync("api/cities");

        // Assert
        Reports.Should().BeEmpty();
        Upstream.Requests.Should().ContainSingle().Which.Options.TryGetValue(ChronicleHTTPHandler.AttemptsKey, out _).Should().BeFalse();
    }

    [Test]
    public async Task Send_FortyTwoCallsSameClient_AggregatedWithNPlusOneHint()
    {
        // Act
        using (Chronicle.Begin("Import"))
        {
            for (var i = 0; i < 42; i++)
            {
                using var response = await Client.GetAsync($"api/cities/{i}");
            }
        }

        // Assert
        Reports.Should().ContainSingle().Which.Message.Should().Contain("\n`- HTTP GET Sample x42 [total ").And.Contain("N+1 suspected");
    }

    [Test]
    public void AddSolHTTPClient_RegistersChronicleAndHandlersInOrder()
    {
        // Act
        var handler = Services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler("Sample");

        // Assert
        var chain = new List<string>();
        for (var current = handler as DelegatingHandler; current is not null; current = current.InnerHandler as DelegatingHandler)
        {
            chain.Add(current.GetType().Name);
        }

        chain.Should().ContainInOrder(
            nameof(ChronicleHTTPHandler), nameof(CorrelationPropagatingHandler), "ResilienceHandler", nameof(ChronicleHTTPAttemptHandler));
        Services.GetService<SolTechnology.Core.Logging.Chronicles.IChronicle>().Should().NotBeNull();
    }
}
