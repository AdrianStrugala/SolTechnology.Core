using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.HTTP.Handlers;

/// <summary>Adds the two Chronicle handlers to a client's pipeline, outside and inside the resilience handler.</summary>
internal static class ChronicleHTTPHandlerRegistration
{
    internal static void AddChronicleCallHandler(IHttpClientBuilder builder, string httpClientName)
        => builder.AddHttpMessageHandler(sp =>
            new ChronicleHTTPHandler(sp.GetRequiredService<IChronicle>(), httpClientName, ClientTimeout(sp, httpClientName)));

    // Mirrors ModuleInstaller.ConfigureClient: Polly owns the deadline, otherwise TimeoutSeconds or HttpClient's 100 s.
    private static TimeSpan ClientTimeout(IServiceProvider sp, string httpClientName)
    {
        if (sp.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get(httpClientName).UsePolly)
        {
            return Timeout.InfiniteTimeSpan;
        }

        var seconds = sp.GetRequiredService<IOptionsMonitor<HTTPClientConfiguration>>().Get(httpClientName).TimeoutSeconds;
        return seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : TimeSpan.FromSeconds(100);
    }

    internal static void AddChronicleAttemptHandler(IHttpClientBuilder builder)
    {
        builder.Services.TryAddTransient<ChronicleHTTPAttemptHandler>();
        builder.AddHttpMessageHandler<ChronicleHTTPAttemptHandler>();
    }
}
