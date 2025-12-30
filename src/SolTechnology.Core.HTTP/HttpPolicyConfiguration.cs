namespace SolTechnology.Core.HTTP;

public class HttpPolicyConfiguration
{
    // all the times are in millis
    public bool UsePolly { get; set; } = true;

    public int RetryTimeout { get; set; } = 200;
    public int RetryInitialDelay { get; set; } = 10;
    public int MaxRequestRetries { get; set; } = 3;

    public double CircuitBreakerFailureThreshold { get; set; } = 0.3;
    public int CircuitBreakerDelayDuration { get; set; } = 10000;
    public int CircuitBreakerSamplingDuration { get; set; } = 60000;
    public int CircuitBreakerMinimumThroughput { get; set; } = 100;

    public int RequestTimeout { get; set; } = 1000;
}