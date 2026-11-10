using System.Runtime.CompilerServices;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.HTTP.Handlers;

/// <summary>
/// Outermost handler of a <c>AddSolHTTPClient</c> client: records one Chronicle step per logical
/// call (<c>HTTP {Method} {clientName}</c>) with the host, final status code and attempt count.
/// The path and query are never recorded, because they can carry personal data.
/// </summary>
internal sealed class ChronicleHTTPHandler(IChronicle chronicle, string clientName) : DelegatingHandler
{
    /// <summary>Attempt counter shared with <see cref="ChronicleHTTPAttemptHandler"/>; request snapshots copy the reference.</summary>
    internal static readonly HttpRequestOptionsKey<StrongBox<int>> AttemptsKey = new("SolTechnology.Core.HTTP.ChronicleAttempts");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var step = chronicle.Step("HTTP " + request.Method.Method + " " + clientName);
        if (!step.IsRecording)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var attempts = new StrongBox<int>();
        request.Options.Set(AttemptsKey, attempts);
        step.Set("Host", request.RequestUri?.Host);
        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;
            step.Set("StatusCode", statusCode).Set("Attempts", attempts.Value);
            if (statusCode >= 500)
            {
                step.Fail("HTTP" + statusCode);
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            step.Set("Attempts", attempts.Value).Cancel();
            throw;
        }
        catch (Exception exception)
        {
            step.Set("Attempts", attempts.Value).Fail(exception);
            throw;
        }
    }
}
