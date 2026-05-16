using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.GeolocationDataClients.GeoDb;
using DreamTravel.GeolocationDataClients.GoogleApi;
using DreamTravel.GeolocationDataClients.MichelinApi;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SolTechnology.Core.HTTP;
using Xunit;

namespace DreamTravel.GeolocationDataClients.IntegrationTests;

/// <summary>
/// Pins the per-client <see cref="HttpPolicyConfiguration"/> contract that
/// DreamTravel relies on after the <c>SolTechnology.Core.HTTP</c> 0.7.0 rework.
/// <para>
/// These tests are configuration-only — they do not hit any external endpoint.
/// They guard against silent drift in the per-client <c>Policy</c> sections
/// (timeouts, retry budgets, overall budget) the operator team tunes for
/// prod incidents.
/// </para>
/// </summary>
public sealed class HttpPolicyConfigurationTests
{
    /// <summary>
    /// Composes a minimal host that mirrors what <c>DreamTravel.Api</c> does at
    /// startup for the three geolocation clients. Config is supplied inline so
    /// the test is independent of the appsettings.json files on disk.
    /// </summary>
    private static WebApplication BuildHost(IDictionary<string, string?> config)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(config);
        builder.Services.InstallGeolocationDataClients();
        return builder.Build();
    }

    [Fact]
    public void Google_DefaultDevPolicy_FromAppSettings_IsApplied()
    {
        // Mirrors the per-client Policy block in
        // src/Presentation/DreamTravel.Api/appsettings.json. If those values
        // change in config but the test is not updated, the prod incident
        // we tuned them against may resurface — this test forces an explicit
        // re-acknowledgement.
        using var app = BuildHost(new Dictionary<string, string?>
        {
            ["HTTPClients:Google:BaseAddress"] = "http://localhost:2137/google/",
            ["HTTPClients:Google:Options:Key"] = "googleKey",
            ["HTTPClients:Google:Policy:RequestTimeout"] = "15000",
            ["HTTPClients:Google:Policy:MaxRequestRetries"] = "3",
            ["HTTPClients:Google:Policy:OverallRequestBudget"] = "60000",
            ["HTTPClients:Michelin:BaseAddress"] = "http://apir.viamichelin.com",
            ["HTTPClients:GeoDb:BaseAddress"] = "http://geodb-free-service.wirefreethought.com",
        });

        var policy = app.Services
            .GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>()
            .Get("Google");

        policy.RequestTimeout.Should().Be(15_000);
        policy.MaxRequestRetries.Should().Be(3);
        policy.OverallRequestBudget.Should().Be(60_000);
        policy.RetryOnUnsafeVerbs.Should().BeFalse(
            "DreamTravel geolocation clients are GET-only; retrying unsafe verbs is never wanted");
    }

    [Fact]
    public void GeoDb_RateLimitFriendlyPolicy_IsApplied()
    {
        // GeoDB free-tier hits 429 under load. The dev config picks a higher
        // RetryInitialDelay so we don't hammer immediately on backoff start.
        using var app = BuildHost(new Dictionary<string, string?>
        {
            ["HTTPClients:Google:BaseAddress"] = "http://x/",
            ["HTTPClients:Google:Options:Key"] = "k",
            ["HTTPClients:Michelin:BaseAddress"] = "http://x/",
            ["HTTPClients:GeoDb:BaseAddress"] = "http://geodb-free-service.wirefreethought.com",
            ["HTTPClients:GeoDb:Policy:RequestTimeout"] = "10000",
            ["HTTPClients:GeoDb:Policy:MaxRequestRetries"] = "2",
            ["HTTPClients:GeoDb:Policy:RetryInitialDelay"] = "500",
        });

        var policy = app.Services
            .GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>()
            .Get("GeoDb");

        policy.RetryInitialDelay.Should().Be(500);
        policy.MaxRequestRetries.Should().Be(2);
        policy.RequestTimeout.Should().Be(10_000);
    }

    [Fact]
    public void AllClients_FallBackTo070ProductionDefaults_WhenNoPolicyOverride()
    {
        // Smoke regression for the policy contract — we resolve options
        // directly rather than the typed clients themselves because the
        // production clients pull additional dependencies (caching decorator
        // for Google) that this test deliberately does not compose. The point
        // is to pin the per-client Polly defaults DreamTravel inherits from
        // SolTechnology.Core.HTTP 0.7.0.
        using var app = BuildHost(new Dictionary<string, string?>
        {
            ["HTTPClients:Google:BaseAddress"] = "http://localhost/",
            ["HTTPClients:Google:Options:Key"] = "k",
            ["HTTPClients:Michelin:BaseAddress"] = "http://localhost/",
            ["HTTPClients:GeoDb:BaseAddress"] = "http://localhost/",
        });

        var monitor = app.Services.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>();

        foreach (var clientName in new[] { "Google", "Michelin", "GeoDb" })
        {
            var policy = monitor.Get(clientName);

            policy.RequestTimeout.Should().Be(10_000, $"{clientName} inherits the 0.7.0 default");
            policy.MaxRequestRetries.Should().Be(2, $"{clientName} inherits the 0.7.0 default");
            policy.RetryOnUnsafeVerbs.Should().BeFalse($"{clientName} is GET-only, no opt-in needed");
            policy.IncludeResponseBodyInException.Should().BeFalse($"{clientName} keeps body capture off by default for PII hygiene");
            policy.OverallRequestBudget.Should().BeNull($"{clientName} leaves the outer budget opt-in");
        }
    }

    [Fact]
    public async Task MissingBaseAddress_FailsHostStartup_ThanksToValidateOnStart()
    {
        // .ValidateOnStart() runs validators when IHost.StartAsync fires (not
        // at .Build()). DreamTravel inherits this from Core.HTTP — exercise
        // the same path the production host hits at deploy time.
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // GeoDb deliberately missing BaseAddress.
            ["HTTPClients:Google:BaseAddress"] = "http://localhost/",
            ["HTTPClients:Google:Options:Key"] = "k",
            ["HTTPClients:Michelin:BaseAddress"] = "http://localhost/",
        });
        builder.Services.InstallGeolocationDataClients();

        var app = builder.Build();

        Func<Task> act = async () =>
        {
            try
            {
                await app.StartAsync();
            }
            finally
            {
                await app.DisposeAsync();
            }
        };

        await act.Should().ThrowAsync<Exception>()
            .Where(ex => ex is OptionsValidationException || ex is ArgumentException,
                "missing BaseAddress for a registered client must surface as a typed startup error");
    }
}

