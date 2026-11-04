using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace SolTechnology.Core.HTTP.Tests;

/// <summary>
/// Every <see cref="HttpPolicyConfiguration"/> boundary is either rejected at host start or
/// accepted by Polly on the first request; nothing passes start only to fail on first use.
/// </summary>
public sealed class HttpPolicyConfigurationTests
{
    private const string OneDay = "86400000";
    private const string OverOneDay = "86400001";

    [TestCase(nameof(HttpPolicyConfiguration.MaxRequestRetries), "0")]
    [TestCase(nameof(HttpPolicyConfiguration.MaxRequestRetries), "100")]
    [TestCase(nameof(HttpPolicyConfiguration.RetryInitialDelay), "0")]
    [TestCase(nameof(HttpPolicyConfiguration.RetryInitialDelay), OneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.RetryTimeout), "1")]
    [TestCase(nameof(HttpPolicyConfiguration.RetryTimeout), OneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.RequestTimeout), "10")]
    [TestCase(nameof(HttpPolicyConfiguration.RequestTimeout), OneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.OverallRequestBudget), OneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerDelayDuration), "500")]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerDelayDuration), OneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerSamplingDuration), "500")]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerSamplingDuration), OneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerMinimumThroughput), "2")]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerFailureThreshold), "0")]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerFailureThreshold), "1")]
    public async Task StartAndFirstRequest_BoundaryInsidePollyRange_Succeeds(string property, string value)
    {
        // Arrange
        using var host = BuildHost(property, value);

        // Act
        await host.StartAsync();
        using var response = await host.Services.GetRequiredService<ISampleHTTPClient>().HttpClient.GetAsync("ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await host.StopAsync();
    }

    [TestCase(nameof(HttpPolicyConfiguration.MaxRequestRetries), "-1")]
    [TestCase(nameof(HttpPolicyConfiguration.MaxRequestRetries), "101")]
    [TestCase(nameof(HttpPolicyConfiguration.RetryInitialDelay), "-1")]
    [TestCase(nameof(HttpPolicyConfiguration.RetryInitialDelay), OverOneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.RetryTimeout), "0")]
    [TestCase(nameof(HttpPolicyConfiguration.RetryTimeout), OverOneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.RequestTimeout), "9")]
    [TestCase(nameof(HttpPolicyConfiguration.RequestTimeout), OverOneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.OverallRequestBudget), OverOneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerDelayDuration), "499")]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerDelayDuration), OverOneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerSamplingDuration), "499")]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerSamplingDuration), OverOneDay)]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerMinimumThroughput), "1")]
    [TestCase(nameof(HttpPolicyConfiguration.CircuitBreakerFailureThreshold), "1.01")]
    public async Task Start_BoundaryOutsidePollyRange_FailsValidation(string property, string value)
    {
        // Arrange
        using var host = BuildHost(property, value);

        // Act
        var act = () => host.StartAsync();

        // Assert
        (await act.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain(property);
    }

    private static IHost BuildHost(string property, string value)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HTTPClients:Sample:BaseAddress"] = "http://upstream.test/",
            [$"HTTPClients:Sample:Policy:{property}"] = value,
        });

        var upstream = new MockHttpMessageHandler();
        upstream.When("*").Respond(HttpStatusCode.OK);

        builder.Services.AddSolHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Sample");
        builder.Services.AddHttpClient("Sample").ConfigurePrimaryHttpMessageHandler(() => upstream);
        return builder.Build();
    }
}
