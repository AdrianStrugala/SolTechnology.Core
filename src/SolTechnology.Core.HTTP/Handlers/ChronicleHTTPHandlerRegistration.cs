using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.HTTP.Handlers;

/// <summary>Adds the two Chronicle handlers to a client's pipeline, outside and inside the resilience handler.</summary>
internal static class ChronicleHTTPHandlerRegistration
{
    internal static void AddChronicleCallHandler(IHttpClientBuilder builder, string httpClientName)
        => builder.AddHttpMessageHandler(sp => new ChronicleHTTPHandler(sp.GetRequiredService<IChronicle>(), httpClientName));

    internal static void AddChronicleAttemptHandler(IHttpClientBuilder builder)
    {
        builder.Services.TryAddTransient<ChronicleHTTPAttemptHandler>();
        builder.AddHttpMessageHandler<ChronicleHTTPAttemptHandler>();
    }
}
