using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using SolTechnology.Core.HTTP.Telemetry;

namespace SolTechnology.Core.HTTP;

internal sealed class HttpPolicyFactory(ILogger<HttpPolicyFactory> logger, HttpClientMetrics metrics)
{
    private readonly ILogger _logger = logger;
    private readonly HttpClientMetrics _metrics = metrics;

    /// <summary>
    /// Adds the (optional outer-budget →) retry → circuit-breaker → per-attempt-timeout
    /// strategy chain onto the supplied <paramref name="builder"/>. <paramref name="clientName"/>
    /// is used as a metric tag and a log-scope value so operators can correlate
    /// resilience events back to a typed client.
    /// <para>
    /// Ordering rationale: the outer timeout (when set via
    /// <see cref="HttpPolicyConfiguration.OverallRequestBudget"/>) bounds the
    /// entire logical call; retries observe both per-attempt timeout-rejections
    /// and breaker exceptions as transient failures.
    /// </para>
    /// </summary>
    public void Configure(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        HttpPolicyConfiguration? configuration,
        string clientName)
    {
        configuration ??= new HttpPolicyConfiguration();

        if (!configuration.UsePolly)
        {
            // Caller already logged the "no resilience" warning at registration
            // time (see ModuleInstaller). Leave the pipeline empty so the
            // resilience handler is a no-op pass-through.
            return;
        }

        // Outer budget: bounds the whole logical call (retries + waits + breaker
        // half-open probes). Added FIRST so it is the OUTERMOST strategy on the
        // pipeline — without that, retries could legitimately exceed the budget.
        if (configuration.OverallRequestBudget is { } budget)
        {
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMilliseconds(budget),
            });
        }

        var retryableStatusCodes = new[]
        {
            HttpStatusCode.RequestTimeout,        // 408
            HttpStatusCode.TooManyRequests,       // 429 — honours Retry-After
            HttpStatusCode.InternalServerError,   // 500
            HttpStatusCode.BadGateway,            // 502
            HttpStatusCode.ServiceUnavailable,    // 503
            HttpStatusCode.GatewayTimeout,        // 504
        };

        // Retry predicate is distinct from the breaker's so we can refuse retries
        // on POST/PATCH while still letting the breaker observe their failures —
        // a 500-storm on POST should still trip the breaker for subsequent GETs.
        ValueTask<bool> ShouldRetry(Outcome<HttpResponseMessage> outcome)
        {
            if (!configuration.RetryOnUnsafeVerbs && IsUnsafeVerb(outcome.Result?.RequestMessage?.Method))
            {
                return ValueTask.FromResult(false);
            }

            return IsTransientFailure(outcome, retryableStatusCodes);
        }

        ValueTask<bool> ShouldBreak(Outcome<HttpResponseMessage> outcome)
            => IsTransientFailure(outcome, retryableStatusCodes);

        builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = configuration.MaxRequestRetries,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromMilliseconds(configuration.RetryInitialDelay),
            MaxDelay = TimeSpan.FromMilliseconds(configuration.RetryTimeout),
            ShouldHandle = args => ShouldRetry(args.Outcome),
            DelayGenerator = args =>
            {
                var retryAfter = args.Outcome.Result?.Headers.RetryAfter;
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
            },
            OnRetry = args =>
            {
                var method = args.Outcome.Result?.RequestMessage?.Method?.Method ?? "?";
                var outcome = args.Outcome.Exception is not null ? "exception" : "status";
                _metrics.Retries.Add(1,
                    new KeyValuePair<string, object?>("client.name", clientName),
                    new KeyValuePair<string, object?>("http.method", method),
                    new KeyValuePair<string, object?>("outcome", outcome));

                _logger.LogWarning(
                    "Retry policy activated for [{ClientName}] in [{Delay}] seconds. Exception: [{Exception}]",
                    clientName,
                    args.RetryDelay.TotalSeconds,
                    args.Outcome.Exception);
                return default;
            },
        });

        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = configuration.CircuitBreakerFailureThreshold,
            SamplingDuration = TimeSpan.FromMilliseconds(configuration.CircuitBreakerSamplingDuration),
            MinimumThroughput = configuration.CircuitBreakerMinimumThroughput,
            BreakDuration = TimeSpan.FromMilliseconds(configuration.CircuitBreakerDelayDuration),
            ShouldHandle = args => ShouldBreak(args.Outcome),
            OnOpened = args =>
            {
                _metrics.CircuitStateChanges.Add(1,
                    new KeyValuePair<string, object?>("client.name", clientName),
                    new KeyValuePair<string, object?>("state", "open"));
                _logger.LogWarning(
                    "Circuit breaker opened for [{ClientName}]. Exception [{Exception}]",
                    clientName,
                    args.Outcome.Exception);
                return default;
            },
            OnClosed = _ =>
            {
                _metrics.CircuitStateChanges.Add(1,
                    new KeyValuePair<string, object?>("client.name", clientName),
                    new KeyValuePair<string, object?>("state", "closed"));
                _logger.LogWarning("Circuit breaker closed for [{ClientName}]", clientName);
                return default;
            },
            OnHalfOpened = _ =>
            {
                _metrics.CircuitStateChanges.Add(1,
                    new KeyValuePair<string, object?>("client.name", clientName),
                    new KeyValuePair<string, object?>("state", "half-open"));
                _logger.LogWarning("Circuit breaker set to partially opened for [{ClientName}]", clientName);
                return default;
            },
        });

        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromMilliseconds(configuration.RequestTimeout),
        });
    }

    private static ValueTask<bool> IsTransientFailure(
        Outcome<HttpResponseMessage> outcome,
        HttpStatusCode[] retryableStatusCodes)
    {
        if (outcome.Exception is HttpRequestException
            or TimeoutRejectedException
            or TaskCanceledException)
        {
            return ValueTask.FromResult(true);
        }

        if (outcome.Result is { } response &&
            Array.IndexOf(retryableStatusCodes, response.StatusCode) >= 0)
        {
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    private static bool IsUnsafeVerb(HttpMethod? method)
    {
        if (method is null)
        {
            return false;
        }

        // RFC 7231: GET/HEAD/OPTIONS are safe; PUT/DELETE are idempotent though
        // not safe; POST/PATCH/CONNECT are neither and replaying them risks
        // duplicating side effects upstream.
        return method == HttpMethod.Post
            || method == HttpMethod.Patch
            || string.Equals(method.Method, "CONNECT", StringComparison.OrdinalIgnoreCase);
    }
}

