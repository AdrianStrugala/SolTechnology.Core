using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace DreamTravel.FunctionalTests;

/// <summary>
/// Pins one resilience pipeline per Sol HTTP client. Aspire ServiceDefaults' default
/// <c>AddStandardResilienceHandler()</c> would wrap the <c>core-http-{name}</c> pipeline and
/// multiply retries, stack timeouts and double the circuit breakers.
/// </summary>
public sealed class HttpResiliencePipelineTests
{
    private static IEnumerable<TestCaseData> Hosts()
    {
        yield return new TestCaseData((Func<IServiceProvider>)(() => ComponentTestsFixture.ApiFixture.TestServer.Services)).SetArgDisplayNames("Api");
        yield return new TestCaseData((Func<IServiceProvider>)(() => ComponentTestsFixture.WorkerFixture.TestServer.Services)).SetArgDisplayNames("Worker");
    }

    [TestCaseSource(nameof(Hosts))]
    public void SolHTTPClients_HaveExactlyOneResilienceHandler(Func<IServiceProvider> services)
    {
        var handlerFactory = services().GetRequiredService<IHttpMessageHandlerFactory>();

        foreach (var clientName in new[] { "Google", "Michelin", "GeoDb" })
        {
            CountResilienceHandlers(handlerFactory.CreateHandler(clientName))
                .Should().Be(1, $"the [{clientName}] client must run only its core-http-{clientName} pipeline");
        }
    }

    private static int CountResilienceHandlers(HttpMessageHandler handler)
    {
        var count = 0;
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
