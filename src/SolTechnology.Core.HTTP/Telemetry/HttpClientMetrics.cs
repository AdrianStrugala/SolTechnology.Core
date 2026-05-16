using System.Diagnostics.Metrics;

namespace SolTechnology.Core.HTTP.Telemetry;

/// <summary>
/// Stable observability surface for <c>SolTechnology.Core.HTTP</c> resilience
/// events. Consumers wire their preferred OpenTelemetry / .NET metrics exporter
/// against the meter name <c>SolTechnology.Core.HTTP</c>; the instrument names
/// below are a documented contract (dashboards / alerts depend on them) and
/// will not change without a MAJOR bump.
/// <para>
/// Sitting on top of <see cref="Meter"/> rather than the implicit Polly meter
/// keeps the contract stable even if the underlying resilience library churns
/// its instrument names.
/// </para>
/// </summary>
public sealed class HttpClientMetrics
{
    /// <summary>Documented meter name. Stable across MINOR releases.</summary>
    public const string MeterName = "SolTechnology.Core.HTTP";

    private readonly Meter _meter;

    /// <summary>
    /// Incremented once per retry attempt. Tags: <c>client.name</c>,
    /// <c>http.method</c>, <c>outcome</c> (<c>exception</c> / <c>status</c>).
    /// </summary>
    public Counter<long> Retries { get; }

    /// <summary>
    /// Incremented once per circuit-breaker state transition. Tags:
    /// <c>client.name</c>, <c>state</c> (<c>open</c> / <c>half-open</c> /
    /// <c>closed</c>).
    /// </summary>
    public Counter<long> CircuitStateChanges { get; }

    public HttpClientMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        Retries = _meter.CreateCounter<long>(
            name: "soltechnology.core.http.retries",
            unit: "{retry}",
            description: "Number of retry attempts performed by the resilience pipeline.");
        CircuitStateChanges = _meter.CreateCounter<long>(
            name: "soltechnology.core.http.circuit_state_changes",
            unit: "{transition}",
            description: "Number of circuit-breaker state transitions.");
    }
}

