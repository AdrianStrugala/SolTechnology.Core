using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SolTechnology.Core.HTTP.Telemetry;

namespace SolTechnology.Core.HTTP;

internal static class RetryStrategyOptionsBuilder
{
    public static RetryStrategyOptions<HttpResponseMessage> Build(
        HttpPolicyConfiguration configuration,
        string clientName,
        ILogger logger,
        HttpClientMetrics metrics) => new()
    {
        MaxRetryAttempts = configuration.MaxRequestRetries,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromMilliseconds(configuration.RetryInitialDelay),
        MaxDelay = TimeSpan.FromMilliseconds(configuration.RetryTimeout),
        ShouldHandle = args => ShouldRetry(configuration, args),
        DelayGenerator = args => RetryAfterDelay(configuration, args.Outcome),
        OnRetry = args =>
        {
            var method = ResolveMethod(args.Context, args.Outcome)?.Method ?? "?";
            var outcome = args.Outcome.Exception is not null ? "exception" : "status";
            metrics.Retries.Add(1,
                new KeyValuePair<string, object?>("client.name", clientName),
                new KeyValuePair<string, object?>("http.method", method),
                new KeyValuePair<string, object?>("outcome", outcome));

            logger.LogWarning(
                args.Outcome.Exception,
                "Retry policy activated for [{ClientName}] in [{Delay}] seconds",
                clientName,
                args.RetryDelay.TotalSeconds);
            return default;
        },
    };

    // Distinct from the breaker predicate: POST/PATCH are not retried, but their failures still trip the breaker.
    private static async ValueTask<bool> ShouldRetry(
        HttpPolicyConfiguration configuration,
        RetryPredicateArguments<HttpResponseMessage> args)
    {
        var outcome = args.Outcome;
        if (!configuration.RetryOnUnsafeVerbs && IsUnsafeVerb(ResolveMethod(args.Context, outcome)))
        {
            return false;
        }

        var isTransient = await HttpPolicyFactory.IsTransientFailure(outcome, args.Context);
        if (!isTransient)
        {
            return false;
        }

        // Polly returns the last attempt's response whatever the predicate says, so it is left unread for the caller.
        if (configuration.RetryPredicate is null || outcome.Result is null || args.AttemptNumber >= configuration.MaxRequestRetries)
        {
            return true;
        }

        // A body over the cap is not inspected; the transient status decides.
        if (!await RequestBuilder.TryBufferCappedBody(outcome.Result.Content, args.Context.CancellationToken))
        {
            return true;
        }

        return await configuration.RetryPredicate(outcome.Result);
    }

    private static ValueTask<TimeSpan?> RetryAfterDelay(HttpPolicyConfiguration configuration, Outcome<HttpResponseMessage> outcome)
    {
        var retryAfter = outcome.Result?.Headers.RetryAfter;
        if (retryAfter is null)
        {
            return new ValueTask<TimeSpan?>((TimeSpan?)null);
        }

        TimeSpan? delay = retryAfter.Delta
            ?? (retryAfter.Date.HasValue
                ? retryAfter.Date.Value - DateTimeOffset.UtcNow
                : null);

        if (delay is null || delay <= TimeSpan.Zero)
        {
            return new ValueTask<TimeSpan?>((TimeSpan?)null);
        }

        var max = TimeSpan.FromMilliseconds(configuration.RetryTimeout);
        if (delay > max)
        {
            delay = max;
        }

        return new ValueTask<TimeSpan?>(delay);
    }

    // An exception outcome has no response, so the request stored by the resilience handler is the reliable source.
    private static HttpMethod? ResolveMethod(ResilienceContext context, Outcome<HttpResponseMessage> outcome)
        => context.GetRequestMessage()?.Method ?? outcome.Result?.RequestMessage?.Method;

    // RFC 7231: POST, PATCH and CONNECT are neither safe nor idempotent, so a replay can duplicate side effects.
    private static bool IsUnsafeVerb(HttpMethod? method)
    {
        if (method is null)
        {
            return false;
        }

        return method == HttpMethod.Post
            || method == HttpMethod.Patch
            || string.Equals(method.Method, "CONNECT", StringComparison.OrdinalIgnoreCase);
    }
}
