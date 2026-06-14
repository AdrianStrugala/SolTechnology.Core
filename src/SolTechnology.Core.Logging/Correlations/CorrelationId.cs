using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace SolTechnology.Core.Logging.Correlations;

/// <summary>
/// A single correlation identifier that aligns with the W3C Trace Context standard
/// used by OpenTelemetry, Application Insights and ASP.NET Core 6+.
///
/// Resolution order on inbound:
/// <list type="number">
///   <item>The trace id of the current <see cref="Activity"/> (populated automatically
///         from the inbound <c>traceparent</c> header by ASP.NET Core).</item>
///   <item>The optional override header <c>X-Correlation-Id</c> (only when no Activity is in scope).</item>
///   <item>A freshly generated 32-character hex string.</item>
/// </list>
/// On outbound the id is echoed on both <c>traceparent</c> (when an Activity is in scope,
/// for OpenTelemetry-aware clients) and <c>X-Correlation-Id</c> (for plain support workflows).
/// </summary>
public sealed record CorrelationId
{
    /// <summary>
    /// Friendly correlation header. Always echoed on the response so clients can quote it
    /// in support tickets without parsing W3C trace context.
    /// </summary>
    public const string HeaderKey = "X-Correlation-Id";

    /// <summary>W3C Trace Context header name. Echoed on response when an Activity is in scope.</summary>
    public const string TraceParentHeaderKey = "traceparent";

    public const int MaxLength = 128;

    /// <summary>Logger-scope property name. Matches App Insights / OTel convention.</summary>
    public const string ScopeKey = "CorrelationId";

    private CorrelationId(string value) => Value = value;

    public string Value { get; }

    /// <summary>Wraps an existing correlation id string (e.g. restored from a job parameter).</summary>
    public static CorrelationId FromString(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new CorrelationId(value);
    }

    /// <summary>Generates a new id, preferring <see cref="Activity.Current"/>'s trace id.</summary>
    public static CorrelationId Generate()
    {
        var activity = Activity.Current;
        if (activity is not null && activity.TraceId != default)
        {
            return new CorrelationId(activity.TraceId.ToHexString());
        }

        return new CorrelationId(Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    /// Reads the correlation id from the request. ASP.NET Core has already parsed
    /// <c>traceparent</c> into <see cref="Activity.Current"/> by the time middleware runs,
    /// so we use that as the source of truth. Falls back to <c>X-Correlation-Id</c> header
    /// only when no Activity is in scope.
    /// </summary>
    /// <param name="request">Incoming HTTP request.</param>
    /// <param name="error">Populated when the override header violates <see cref="MaxLength"/>.</param>
    public static CorrelationId FromRequest(HttpRequest request, out string? error)
    {
        error = null;

        var activity = Activity.Current;
        if (activity is not null && activity.TraceId != default)
        {
            return new CorrelationId(activity.TraceId.ToHexString());
        }

        var headerValue = request.Headers[HeaderKey].ToString();
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            if (headerValue.Length > MaxLength)
            {
                error = $"{HeaderKey} must not exceed {MaxLength} characters.";
                return Generate();
            }

            return new CorrelationId(headerValue);
        }

        return Generate();
    }

    /// <summary>
    /// Adds correlation headers to the response:
    /// <list type="bullet">
    ///   <item><c>X-Correlation-Id</c> — always echoed, simple opaque value.</item>
    ///   <item><c>traceparent</c> — echoed in W3C Trace Context format
    ///         (<c>00-{trace-id}-{span-id}-{flags}</c>) when an <see cref="Activity"/> is in scope,
    ///         so OpenTelemetry-aware downstreams / clients can continue the trace.</item>
    /// </list>
    /// Uses <c>TryAdd</c> on <see cref="IHeaderDictionary"/> so any header set earlier in the pipeline wins.
    /// </summary>
    public void EnrichResponse(HttpResponse response)
    {
        response.Headers.TryAdd(HeaderKey, Value);

        var traceParent = TryBuildTraceParent();
        if (traceParent is not null)
        {
            response.Headers.TryAdd(TraceParentHeaderKey, traceParent);
        }
    }

    /// <summary>Builds the dictionary pushed into <c>ILogger.BeginScope</c>.</summary>
    public IDictionary<string, object?> GetScope()
        => new Dictionary<string, object?>(1) { [ScopeKey] = Value };

    public override string ToString() => Value;

    /// <summary>
    /// Renders <see cref="Activity.Current"/> as a W3C <c>traceparent</c> header value
    /// (<c>00-{trace-id}-{parent-id}-{flags}</c>). Returns <c>null</c> when no Activity
    /// is in scope or its trace id is uninitialised.
    /// <para>
    /// Shared between <see cref="EnrichResponse"/> (inbound side, owned by
    /// <c>Core.Logging</c>) and the outbound correlation handler in
    /// <c>SolTechnology.Core.HTTP</c>. Two callers, one implementation — keeps the
    /// header format from drifting.
    /// </para>
    /// </summary>
    public static string? TryBuildTraceParentFromCurrentActivity()
    {
        var activity = Activity.Current;
        if (activity is null || activity.TraceId == default)
        {
            return null;
        }

        var flags = ((activity.ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0) ? "01" : "00";
        return $"00-{activity.TraceId.ToHexString()}-{activity.SpanId.ToHexString()}-{flags}";
    }

    /// <summary>
    /// Renders the current Activity as a W3C <c>traceparent</c> value
    /// (<c>00-{trace-id}-{parent-id}-{flags}</c>). Returns <c>null</c> when no Activity is in scope
    /// or the id was sourced from the legacy <c>X-Correlation-Id</c> header (no real trace context).
    /// </summary>
    private string? TryBuildTraceParent()
    {
        var activity = Activity.Current;
        if (activity is null || activity.TraceId == default)
        {
            return null;
        }

        // Only emit traceparent when our Value is the actual trace id - otherwise we'd be lying
        // about a span id that has nothing to do with the correlation id we just echoed.
        if (!string.Equals(Value, activity.TraceId.ToHexString(), StringComparison.Ordinal))
        {
            return null;
        }

        return TryBuildTraceParentFromCurrentActivity();
    }
}
