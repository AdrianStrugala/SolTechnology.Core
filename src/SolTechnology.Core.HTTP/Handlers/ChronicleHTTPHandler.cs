using System.Diagnostics;
using System.Runtime.CompilerServices;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.HTTP.Handlers;

/// <summary>
/// Outermost handler of a <c>AddSolHTTPClient</c> client: records one Chronicle step per logical
/// call (<c>HTTP {Method} {clientName}</c>) with the host, final status code and attempt count.
/// The path and query are never recorded, because they can carry personal data. A cancellation
/// after <paramref name="clientTimeout"/> (the client's <c>HttpClient.Timeout</c>) is a failed
/// <c>TimeoutException</c>, not a caller cancellation: the handler only sees HttpClient's linked token.
/// </summary>
internal sealed class ChronicleHTTPHandler(IChronicle chronicle, string clientName, TimeSpan clientTimeout) : DelegatingHandler
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

        var started = Stopwatch.GetTimestamp();
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
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            step.Set("Attempts", attempts.Value);
            if (exception.InnerException is TimeoutException || IsClientTimeout(started))
            {
                step.Fail(nameof(TimeoutException));
            }
            else
            {
                step.Cancel();
            }

            throw;
        }
        catch (Exception exception)
        {
            step.Set("Attempts", attempts.Value).Fail(exception);
            throw;
        }
    }

    private bool IsClientTimeout(long started)
        => clientTimeout != Timeout.InfiniteTimeSpan && Stopwatch.GetElapsedTime(started) >= clientTimeout;
}
