using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.HTTP;

namespace DreamTravel.GeolocationDataClients.IntegrationTests;

[TestFixture]
public sealed class HttpPolicyConfigurationTests
{
    private static WebApplication BuildHost(IDictionary<string, string?> config)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(config);
        builder.Services.InstallGeolocationDataClients();
        return builder.Build();
    }

    [Test]
    public void Google_DefaultDevPolicy_FromAppSettings_IsApplied()
    {
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

    [Test]
    public void GeoDb_RateLimitFriendlyPolicy_IsApplied()
    {
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

    [Test]
    public void AllClients_FallBackTo070ProductionDefaults_WhenNoPolicyOverride()
    {
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

    [Test]
    public async Task MissingBaseAddress_FailsHostStartup_ThanksToValidateOnStart()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
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

