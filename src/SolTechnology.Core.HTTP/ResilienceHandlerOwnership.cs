using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.HTTP;

/// <summary>
/// Keeps the Sol pipeline the only resilience layer on a client that uses it, so a default
/// handler (Aspire ServiceDefaults' <c>ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler())</c>)
/// does not wrap it and multiply retries, stack timeouts and double the circuit breakers.
/// </summary>
internal static class ResilienceHandlerOwnership
{
    /// <summary>
    /// Removes every <see cref="ResilienceHandler"/> added before this call when the client's
    /// <see cref="HttpPolicyConfiguration.UsePolly"/> is on; with it off, the app's handlers stay.
    /// </summary>
    /// <remarks>
    /// The same removal as <c>RemoveAllResilienceHandlers()</c>, but decided per handler build because
    /// <c>UsePolly</c> may come from configuration. Microsoft.Extensions.Http applies
    /// <c>ConfigureHttpClientDefaults</c> callbacks before the named client's own, whatever the
    /// registration order, so they are removed; a handler added on the named client after
    /// <c>AddSolHTTPClient</c> is an explicit choice and stays.
    /// </remarks>
    internal static void RemoveForeignHandlersWhenPollyIsOn(IHttpClientBuilder builder, string httpClientName)
        => builder.ConfigureAdditionalHttpMessageHandlers((handlers, sp) =>
        {
            var policy = sp.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get(httpClientName);
            if (!policy.UsePolly)
            {
                return;
            }

            for (var i = handlers.Count - 1; i >= 0; i--)
            {
                if (handlers[i] is ResilienceHandler)
                {
                    handlers.RemoveAt(i);
                }
            }
        });
}
