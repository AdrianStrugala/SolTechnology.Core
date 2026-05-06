using System.Diagnostics;

namespace SolTechnology.Core.Logging.Operations;

/// <summary>
/// Named <see cref="ActivitySource"/>s emitted by <c>SolTechnology.Core.Logging</c> /
/// <c>SolTechnology.Core.CQRS</c>. Subscribe to them in the OpenTelemetry setup of the
/// host application:
/// <code>
/// services.AddOpenTelemetry()
///         .WithTracing(tracing => tracing
///             .AddSource(CoreLoggingActivitySources.OperationsName)
///             .AddAspNetCoreInstrumentation()
///             .AddHttpClientInstrumentation()
///             .AddOtlpExporter()); // or AddAzureMonitorTraceExporter()
/// </code>
/// When no listener is attached, <see cref="ActivitySource.StartActivity(string, ActivityKind)"/>
/// returns <c>null</c> and the per-request overhead collapses to one null-conditional access —
/// libraries that don't opt into OpenTelemetry pay nothing.
/// </summary>
public static class CoreLoggingActivitySources
{
    /// <summary>
    /// Emits one <see cref="Activity"/> per logical operation tracked by the MediatR
    /// pipeline behavior in <c>SolTechnology.Core.CQRS</c> (one Activity per request type).
    /// Tags mirror the <c>[LogScope]</c>-projected scope properties so trace UI and structured
    /// logs use identical keys.
    /// </summary>
    public const string OperationsName = "SolTechnology.Core.Logging.Operations";

    /// <summary>Internal handle used by the pipeline behavior. Not part of the public API.</summary>
    internal static readonly ActivitySource Operations = new(OperationsName);
}

