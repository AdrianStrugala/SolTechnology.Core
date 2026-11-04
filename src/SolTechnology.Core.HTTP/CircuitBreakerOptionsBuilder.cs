using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using SolTechnology.Core.HTTP.Telemetry;

namespace SolTechnology.Core.HTTP;

internal static class CircuitBreakerOptionsBuilder
{
    public static CircuitBreakerStrategyOptions<HttpResponseMessage> Build(
        HttpPolicyConfiguration configuration,
        string clientName,
        ILogger logger,
        HttpClientMetrics metrics) => new()
    {
        FailureRatio = configuration.CircuitBreakerFailureThreshold,
        SamplingDuration = TimeSpan.FromMilliseconds(configuration.CircuitBreakerSamplingDuration),
        MinimumThroughput = configuration.CircuitBreakerMinimumThroughput,
        BreakDuration = TimeSpan.FromMilliseconds(configuration.CircuitBreakerDelayDuration),
        ShouldHandle = args => HttpPolicyFactory.IsTransientFailure(args.Outcome, args.Context),
        OnOpened = args =>
        {
            RecordState(metrics, clientName, "open");
            logger.LogWarning(args.Outcome.Exception, "Circuit breaker opened for [{ClientName}]", clientName);
            return default;
        },
        OnClosed = _ =>
        {
            RecordState(metrics, clientName, "closed");
            logger.LogWarning("Circuit breaker closed for [{ClientName}]", clientName);
            return default;
        },
        OnHalfOpened = _ =>
        {
            RecordState(metrics, clientName, "half-open");
            logger.LogWarning("Circuit breaker set to partially opened for [{ClientName}]", clientName);
            return default;
        },
    };

    private static void RecordState(HttpClientMetrics metrics, string clientName, string state)
        => metrics.CircuitStateChanges.Add(1,
            new KeyValuePair<string, object?>("client.name", clientName),
            new KeyValuePair<string, object?>("state", state));
}
