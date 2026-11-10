using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using NUnit.Framework;

namespace SolTechnology.Core.HTTP.Tests;

/// <summary>
/// Pins the Sol pipeline as the only resilience layer on a Sol client under app-wide defaults such as
/// Aspire ServiceDefaults' <c>ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler())</c>.
/// </summary>
public sealed class ForeignResilienceHandlerTests
{
    private const string ClientName = "Sample";

    private ServiceProvider? _provider;
    private ScriptedHandler _upstream = null!;

    [SetUp]
    public void SetUp() => _upstream = new ScriptedHandler().Then(HttpStatusCode.InternalServerError);

    [TearDown]
    public void TearDown()
    {
        _provider?.Dispose();
        _upstream.Dispose();
    }

    [TestCase(true, TestName = "Defaults registered before AddSolHTTPClient")]
    [TestCase(false, TestName = "Defaults registered after AddSolHTTPClient")]
    public async Task StandardDefault_UsePollyOn_OnlySolPipelineRuns(bool defaultsFirst)
    {
        Build(usePolly: true, defaultsFirst, http => http.AddStandardResilienceHandler(ZeroRetryDelay));

        CountResilienceHandlers(ClientName).Should().Be(1, "ConfigureHttpClientDefaults runs before the named client's configuration whatever the registration order");
        await SendFailingCall();
        _upstream.Requests.Should().HaveCount(3, "the Sol policy makes 3 attempts and nothing multiplies them");
    }

    [Test]
    public async Task HedgingDefault_UsePollyOn_IsRemoved()
    {
        Build(usePolly: true, defaultsFirst: true, http => http.AddStandardHedgingHandler());

        CountResilienceHandlers(ClientName).Should().Be(1);
        await SendFailingCall();
        _upstream.Requests.Should().HaveCount(3);
    }

    [Test]
    public async Task StandardDefault_UsePollyOff_DefaultHandlerStays()
    {
        Build(usePolly: false, defaultsFirst: true, http => http.AddStandardResilienceHandler(ZeroRetryDelay));

        CountResilienceHandlers(ClientName).Should().Be(2, "the empty Sol pipeline sits next to the app's default handler");
        await SendFailingCall();
        _upstream.Requests.Should().HaveCount(4, "the standard handler's 1 + 3 retries apply when the Sol pipeline is off");
    }

    [Test]
    public void StandardDefault_NonSolClient_KeepsDefaultHandler()
    {
        Build(usePolly: true, defaultsFirst: true, http => http.AddStandardResilienceHandler());

        CountResilienceHandlers("Other").Should().Be(1);
    }

    [Test]
    public void HandlerAddedAfterAddSolHTTPClient_OnTheNamedClient_IsKept()
    {
        Build(usePolly: true, defaultsFirst: true, _ => { }, services => services.AddHttpClient(ClientName).AddStandardResilienceHandler());

        CountResilienceHandlers(ClientName).Should().Be(2, "an explicit handler on the named client after AddSolHTTPClient is the app's choice");
    }

    private static void ZeroRetryDelay(HttpStandardResilienceOptions options) => options.Retry.Delay = TimeSpan.Zero;

    private void Build(bool usePolly, bool defaultsFirst, Action<IHttpClientBuilder> defaults, Action<IServiceCollection>? afterSol = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build()).AddLogging();
        if (defaultsFirst)
        {
            services.ConfigureHttpClientDefaults(defaults);
        }

        services.AddSolHTTPClient<ISampleHTTPClient, SampleHTTPClient>(
            ClientName,
            new HTTPClientConfiguration { BaseAddress = "http://api.test/" },
            new HttpPolicyConfiguration { UsePolly = usePolly, RetryInitialDelay = 0 });
        services.AddHttpClient(ClientName).ConfigurePrimaryHttpMessageHandler(() => _upstream);
        services.AddHttpClient("Other");

        afterSol?.Invoke(services);

        if (!defaultsFirst)
        {
            services.ConfigureHttpClientDefaults(defaults);
        }

        _provider = services.BuildServiceProvider();
    }

    private async Task SendFailingCall()
    {
        var client = _provider!.GetRequiredService<ISampleHTTPClient>().HttpClient;
        using var response = await client.GetAsync("api/cities");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private int CountResilienceHandlers(string clientName)
    {
        var count = 0;
        var handler = _provider!.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(clientName);
        for (var current = handler as DelegatingHandler; current is not null; current = current.InnerHandler as DelegatingHandler)
        {
            if (current is ResilienceHandler)
            {
                count++;
            }
        }

        return count;
    }
}
