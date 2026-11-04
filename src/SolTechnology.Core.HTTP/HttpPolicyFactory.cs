using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using SolTechnology.Core.HTTP.Telemetry;

namespace SolTechnology.Core.HTTP;

internal sealed class HttpPolicyFactory(ILogger<HttpPolicyFactory> logger, HttpClientMetrics metrics)
{
    private static readonly HttpStatusCode[] RetryableStatusCodes =
    [
        HttpStatusCode.RequestTimeout,        // 408
        HttpStatusCode.TooManyRequests,       // 429 — honours Retry-After
        HttpStatusCode.InternalServerError,   // 500
        HttpStatusCode.BadGateway,            // 502
        HttpStatusCode.ServiceUnavailable,    // 503
        HttpStatusCode.GatewayTimeout,        // 504
    ];

    /// <summary>
    /// Adds the (optional outer-budget →) retry → circuit-breaker → per-attempt-timeout
    /// strategy chain onto the supplied <paramref name="builder"/>. <paramref name="clientName"/>
    /// is used as a metric tag and a log-scope value so operators can correlate
    /// resilience events back to a typed client.
    /// <para>
    /// Ordering rationale: the outer timeout (when set via
    /// <see cref="HttpPolicyConfiguration.OverallRequestBudget"/>) bounds the
    /// entire logical call; retries treat a per-attempt timeout rejection as a
    /// transient failure, while an open circuit (<see cref="BrokenCircuitException"/>)
    /// is not retried.
    /// </para>
    /// </summary>
    public void Configure(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        HttpPolicyConfiguration? configuration,
        string clientName)
    {
        configuration ??= new HttpPolicyConfiguration();

        // ModuleInstaller already logged the "no resilience" warning; an empty pipeline passes through.
        if (!configuration.UsePolly)
        {
            return;
        }

        // Added first so it is the outermost strategy and bounds retries too.
        if (configuration.OverallRequestBudget is { } budget)
        {
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMilliseconds(budget),
            });
        }

        builder.AddRetry(RetryStrategyOptionsBuilder.Build(configuration, clientName, logger, metrics));
        builder.AddCircuitBreaker(CircuitBreakerOptionsBuilder.Build(configuration, clientName, logger, metrics));
        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromMilliseconds(configuration.RequestTimeout),
        });
    }

    internal static ValueTask<bool> IsTransientFailure(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Exception is HttpRequestException
            or TimeoutRejectedException
            or TaskCanceledException)
        {
            return ValueTask.FromResult(true);
        }

        if (outcome.Result is { } response &&
            Array.IndexOf(RetryableStatusCodes, response.StatusCode) >= 0)
        {
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }
}
