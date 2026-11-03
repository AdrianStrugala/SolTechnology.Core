using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace SolTechnology.Core.Logging.Correlations;

/// <summary>
/// A single correlation identifier that aligns with the W3C Trace Context standard
/// used by OpenTelemetry, Application Insights and ASP.NET Core 6+.
///
/// Resolution order on inbound:
/// <list type="number">
///   <item>The trace id of the current <see cref="Activity"/> when the request carried trace context
///         (a <c>traceparent</c> header, or an Activity with a remote parent).</item>
///   <item>The header <c>X-Correlation-Id</c>, accepted only when it contains <c>[A-Za-z0-9._:-]</c>
///         and is at most <see cref="MaxLength"/> long.</item>
///   <item>The trace id of the current <see cref="Activity"/> (a new trace started by the host).</item>
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
    /// Reads the correlation id from the request. Inbound trace context wins: ASP.NET Core has already
    /// parsed <c>traceparent</c> into <see cref="Activity.Current"/>. Without it, a valid
    /// <c>X-Correlation-Id</c> wins over the trace the host just started, so a caller's id survives
    /// even when the host creates a request Activity.
    /// </summary>
    /// <param name="request">Incoming HTTP request.</param>
    /// <param name="error">
    /// Populated when the override header is longer than <see cref="MaxLength"/> or contains a character
    /// outside <c>[A-Za-z0-9._:-]</c>; a new id is generated then. Never contains the rejected value.
    /// </param>
    public static CorrelationId FromRequest(HttpRequest request, out string? error)
    {
        error = null;

        var activity = Activity.Current;
        if (activity is not null && activity.TraceId != default && HasInboundTraceContext(request, activity))
        {
            return new CorrelationId(activity.TraceId.ToHexString());
        }

        var headerValue = request.Headers[HeaderKey].ToString();
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return Generate();
        }

        error = CorrelationIdValidator.Validate(headerValue);
        return error is null ? new CorrelationId(headerValue) : Generate();
    }

    private static bool HasInboundTraceContext(HttpRequest request, Activity activity)
        => activity.HasRemoteParent || request.Headers.ContainsKey(TraceParentHeaderKey);

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
    /// is in scope or its trace id is uninitialised. Also used by the outbound correlation
    /// handler in <c>SolTechnology.Core.HTTP</c>, so both directions share one header format.
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
    /// The current <c>traceparent</c>, only when <see cref="Value"/> is the current trace id; an id
    /// taken from <c>X-Correlation-Id</c> has no trace context, so no span id is invented for it.
    /// </summary>
    private string? TryBuildTraceParent()
        => Activity.Current is { } activity
           && activity.TraceId != default
           && string.Equals(Value, activity.TraceId.ToHexString(), StringComparison.Ordinal)
            ? TryBuildTraceParentFromCurrentActivity()
            : null;
}
