using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Timeout;

namespace SolTechnology.Core.HTTP;

public class HttpPolicyFactory
{
    private readonly ILogger _logger;

    public HttpPolicyFactory(ILogger<HttpPolicyFactory> logger) => _logger = logger;

    public IAsyncPolicy<HttpResponseMessage> Create(HttpPolicyConfiguration? configuration)
    {
        configuration ??= new();
        
        if (!configuration.UsePolly)
        {
            return Policy.NoOpAsync<HttpResponseMessage>();
        }

        HttpStatusCode[] httpStatusCodesWorthRetrying = {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };

        var backoff = Backoff
            .DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(configuration.RetryInitialDelay), configuration.MaxRequestRetries)
            .Select(s => TimeSpan.FromTicks(Math.Min(s.Ticks, TimeSpan.FromMilliseconds(configuration.RetryTimeout).Ticks)));

        var retry = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .OrResult(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
            .WaitAndRetryAsync(
                backoff,
                (ex, delay) => _logger.LogWarning($"Retry policy activated in [{delay}] seconds. Exception: [{ex}]", delay.TotalSeconds, ex));

        var circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .AdvancedCircuitBreakerAsync(
                configuration.CircuitBreakerFailureThreshold,
                TimeSpan.FromMilliseconds(configuration.CircuitBreakerSamplingDuration),
                configuration.CircuitBreakerMinimumThroughput,
                TimeSpan.FromMilliseconds(configuration.CircuitBreakerDelayDuration),
                (ex, _) => _logger.LogWarning("Circuit breaker opened. Exception [{ex}]", ex),
                () => _logger.LogWarning("Circuit breaker closed"),
                () => _logger.LogWarning("Circuit breaker set to partially opened"));

        var timeout = Policy.TimeoutAsync(configuration.RequestTimeout);

        return retry
            .WrapAsync(circuitBreakerPolicy)
            .WrapAsync(timeout);
    }
}
