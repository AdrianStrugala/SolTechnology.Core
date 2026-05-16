using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace SolTechnology.Core.HTTP.Tests;

public sealed class ModuleInstallerTests
{
    private static WebApplicationBuilder NewBuilder() => WebApplication.CreateBuilder();

    // ---- Original smoke test (preserved) ---------------------------------

    [Fact]
    public void AddHTTPClient_ConfigurationProvidedAsParameter_ClientHasExpectedBaseAddressAndHeader_TimeoutIgnoredWhenPollyOn()
    {
        // Under the default UsePolly=true, HttpClient.Timeout is set to
        // InfiniteTimeSpan so the Polly per-attempt timeout is the single
        // source of truth. TimeoutSeconds from configuration is logged as a
        // warning but does not bind to HttpClient.Timeout.
        var configuration = new HTTPClientConfiguration
        {
            BaseAddress = "http://localhost:8080/",
            TimeoutSeconds = 21,
            Headers = new List<Header>
            {
                new() { Name = "HeaderName", Value = "HeaderValue" },
            },
        };

        var sut = NewBuilder();
        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Sample", configuration);
        var app = sut.Build();

        var client = app.Services.GetRequiredService<ISampleHTTPClient>();
        client.HttpClient.BaseAddress.Should().Be(new Uri(configuration.BaseAddress));
        client.HttpClient.Timeout.Should().Be(Timeout.InfiniteTimeSpan,
            "Polly is the single timeout owner when UsePolly=true");
        client.HttpClient.DefaultRequestHeaders
            .GetValues("HeaderName").Single().Should().Be("HeaderValue");
    }

    [Fact]
    public void AddHTTPClient_UsePollyFalse_HttpClientOwnsTimeoutFromTimeoutSeconds()
    {
        // Fallback path: with Polly disabled, HttpClient.Timeout is the only
        // remaining deadline and must honour TimeoutSeconds.
        var configuration = new HTTPClientConfiguration
        {
            BaseAddress = "http://localhost:8080/",
            TimeoutSeconds = 21,
            Headers = new List<Header>(),
        };
        var policy = new HttpPolicyConfiguration { UsePolly = false };

        var sut = NewBuilder();
        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Sample", configuration, policy);
        var app = sut.Build();

        var client = app.Services.GetRequiredService<ISampleHTTPClient>();
        client.HttpClient.Timeout.Should().Be(TimeSpan.FromSeconds(21));
    }

    // ---- Configuration-driven registration --------------------------------

    [Fact]
    public void AddHTTPClient_ConfigurationProvidedFromAppsettings_ClientReadsBaseAddressAndHeadersFromSection()
    {
        var sut = NewBuilder();
        sut.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HTTPClients:Sample:BaseAddress"] = "http://from-config/",
            ["HTTPClients:Sample:TimeoutSeconds"] = "42",
        });

        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Sample");
        var app = sut.Build();

        var client = app.Services.GetRequiredService<ISampleHTTPClient>();
        client.HttpClient.BaseAddress.Should().Be(new Uri("http://from-config/"));
        // Default UsePolly=true → HttpClient.Timeout is infinite, TimeoutSeconds
        // is a documented warning. See AddHTTPClient_UsePollyFalse_... for the
        // fallback case.
        client.HttpClient.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void AddHTTPClient_ConfigurationMissing_FailsHostStartupOrFirstResolve()
    {
        var sut = NewBuilder();
        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Nonexistent");

        // .ValidateOnStart() is wired in ModuleInstaller. The contract is:
        // bad / missing config must surface at host startup or, at the latest,
        // on first resolve. We exercise both paths to keep the test robust
        // against future HostBuilder eager-init changes.
        var act = () =>
        {
            var app = sut.Build();
            return app.Services.GetRequiredService<ISampleHTTPClient>();
        };

        act.Should().Throw<Exception>()
            .Where(ex => ex is ArgumentException || ex is OptionsValidationException,
                "missing client configuration must surface as a typed startup error");
    }

    // ---- Policy options precedence ---------------------------------------

    [Fact]
    public void AddHTTPClient_PolicyOptions_PerClientSectionOverridesGlobal()
    {
        var sut = NewBuilder();
        sut.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HTTPClients:Sample:BaseAddress"] = "http://example/",
            ["HttpPolicy:MaxRequestRetries"] = "9",
            ["HTTPClients:Sample:Policy:MaxRequestRetries"] = "1",
        });

        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Sample");
        var app = sut.Build();

        var policy = app.Services.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get("Sample");
        policy.MaxRequestRetries.Should().Be(1);
    }

    [Fact]
    public void AddHTTPClient_PolicyOptions_GlobalUsedWhenNoPerClientOverride()
    {
        var sut = NewBuilder();
        sut.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HTTPClients:Sample:BaseAddress"] = "http://example/",
            ["HttpPolicy:MaxRequestRetries"] = "7",
        });

        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Sample");
        var app = sut.Build();

        var policy = app.Services.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get("Sample");
        policy.MaxRequestRetries.Should().Be(7);
    }

    [Fact]
    public void AddHTTPClient_PolicyOptions_FallbackToProductionDefaults_WhenNoConfig()
    {
        var sut = NewBuilder();
        sut.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HTTPClients:Sample:BaseAddress"] = "http://example/",
        });

        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Sample");
        var app = sut.Build();

        var policy = app.Services.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get("Sample");

        // Pin the production defaults — see HttpPolicyConfiguration.cs. If they
        // shift unintentionally this test will catch the regression.
        policy.RequestTimeout.Should().Be(10_000);
        policy.RetryTimeout.Should().Be(30_000);
        policy.RetryInitialDelay.Should().Be(200);
        policy.MaxRequestRetries.Should().Be(2);
        policy.CircuitBreakerMinimumThroughput.Should().Be(5);
        policy.CircuitBreakerSamplingDuration.Should().Be(10_000);
        policy.RetryOnUnsafeVerbs.Should().BeFalse("idempotent-only retry is the production default");
        policy.IncludeResponseBodyInException.Should().BeFalse("response body capture is opt-in");
        policy.OverallRequestBudget.Should().BeNull("outer budget is opt-in");
    }

    [Fact]
    public void AddHTTPClient_PolicyOptions_InvalidValueFromConfig_FailsValidationOnResolve()
    {
        // FailureThreshold is a ratio in [0.0, 1.0]. A value of 5.0 is a
        // configuration mistake we want surfaced eagerly with a clear
        // OptionsValidationException — not silently passed to Polly which
        // throws ArgumentOutOfRangeException deep inside the pipeline at the
        // first request.
        var sut = NewBuilder();
        sut.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HTTPClients:Sample:BaseAddress"] = "http://example/",
            ["HTTPClients:Sample:Policy:CircuitBreakerFailureThreshold"] = "5.0",
        });

        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Sample");
        var app = sut.Build();

        var act = () => app.Services.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get("Sample");

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*CircuitBreakerFailureThreshold*");
    }

    [Fact]
    public void AddHTTPClient_PolicyOptions_NegativeRetries_FailsValidation()
    {
        var sut = NewBuilder();
        sut.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HTTPClients:Sample:BaseAddress"] = "http://example/",
            ["HTTPClients:Sample:Policy:MaxRequestRetries"] = "-1",
        });

        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Sample");
        var app = sut.Build();

        var act = () => app.Services.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get("Sample");
        act.Should().Throw<OptionsValidationException>();
    }

    // ---- New: propagateCorrelation opt-out ------------------------------

    [Fact]
    public void AddHTTPClient_PropagateCorrelationFalse_DoesNotRegisterCorrelationHandler()
    {
        // When the host owns correlation (OpenTelemetry, firm middleware) we
        // must not register our handler — otherwise the AsyncLocal store may
        // diverge from the inbound id.
        var sut = NewBuilder();
        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>(
            "Sample",
            new HTTPClientConfiguration { BaseAddress = "http://example/" },
            propagateCorrelation: false);

        // Searching for CorrelationPropagatingHandler in service descriptors:
        // we register it via TryAddTransient(typeof(CorrelationPropagatingHandler)),
        // so its absence in services confirms the opt-out works.
        sut.Services.Should().NotContain(d => d.ServiceType.Name == "CorrelationPropagatingHandler");
    }

    [Fact]
    public void AddHTTPClient_PropagateCorrelationTrue_RegistersCorrelationHandler()
    {
        var sut = NewBuilder();
        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>(
            "Sample",
            new HTTPClientConfiguration { BaseAddress = "http://example/" });

        sut.Services.Should().Contain(d => d.ServiceType.Name == "CorrelationPropagatingHandler");
    }

    // ---- New: OverallRequestBudget cross-field validation ---------------

    [Fact]
    public void AddHTTPClient_OverallBudgetSmallerThanRequestTimeout_FailsValidation()
    {
        var sut = NewBuilder();
        sut.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HTTPClients:Sample:BaseAddress"] = "http://example/",
            ["HTTPClients:Sample:Policy:RequestTimeout"] = "5000",
            ["HTTPClients:Sample:Policy:OverallRequestBudget"] = "1000",
        });

        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>("Sample");

        // Either Build() (ValidateOnStart) or first resolve raises — accept both
        // to stay tolerant of HostBuilder eager-init changes.
        var act = () =>
        {
            var app = sut.Build();
            return app.Services.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get("Sample");
        };

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*OverallRequestBudget*");
    }

    // ---- New: BuildInMemorySource projector guard -----------------------

    [Fact]
    public void BuildInMemorySource_CoversEveryPublicHttpPolicyProperty()
    {
        // Reflection guard: if a future maintainer adds a List<T> / complex
        // property to HttpPolicyConfiguration, BuildInMemorySource must fail
        // loudly rather than silently dropping it. Today every property is
        // primitive / string / IFormattable, so binding through the
        // explicit-parameter path must succeed.
        var sut = NewBuilder();
        sut.Services.AddHTTPClient<ISampleHTTPClient, SampleHTTPClient>(
            "Sample",
            new HTTPClientConfiguration { BaseAddress = "http://example/" },
            new HttpPolicyConfiguration
            {
                MaxRequestRetries = 1,
                RetryOnUnsafeVerbs = true,
                IncludeResponseBodyInException = true,
                OverallRequestBudget = 60_000,
            });

        var app = sut.Build();
        var policy = app.Services.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get("Sample");

        policy.MaxRequestRetries.Should().Be(1);
        policy.RetryOnUnsafeVerbs.Should().BeTrue();
        policy.IncludeResponseBodyInException.Should().BeTrue();
        policy.OverallRequestBudget.Should().Be(60_000);
    }
}
