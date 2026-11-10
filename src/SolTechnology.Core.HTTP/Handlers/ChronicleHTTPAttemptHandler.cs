namespace SolTechnology.Core.HTTP.Handlers;

/// <summary>
/// Innermost Chronicle handler, below the resilience handler: counts every attempt of a call that
/// <see cref="ChronicleHTTPHandler"/> records, so retries show as one step with <c>Attempts</c>.
/// </summary>
internal sealed class ChronicleHTTPAttemptHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Options.TryGetValue(ChronicleHTTPHandler.AttemptsKey, out var attempts))
        {
            Interlocked.Increment(ref attempts.Value);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
