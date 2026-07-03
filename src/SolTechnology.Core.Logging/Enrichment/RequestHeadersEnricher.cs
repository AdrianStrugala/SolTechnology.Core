using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace SolTechnology.Core.Logging.Enrichment;

/// <summary>
/// Built-in <see cref="ILogScopeEnricher"/> that, when
/// <see cref="LoggingOptions.LogRequestHeaders"/> is enabled, projects the inbound
/// request headers into the per-request log scope under key <c>RequestHeaders</c>.
/// Headers whose name matches <see cref="LoggingOptions.MaskedHeaders"/> (case-insensitive)
/// and any value starting with <c>Bearer </c> are replaced with
/// <see cref="LoggingDefaults.MaskedValue"/>.
/// </summary>
/// <remarks>
/// Auto-registered by <c>AddSolLogging</c>. Pays nothing when the option is off
/// (the <see cref="Enrich"/> implementation short-circuits in O(1)).
/// </remarks>
internal sealed class RequestHeadersEnricher : ILogScopeEnricher
{
    private const string ScopeKey = "RequestHeaders";
    private const string BearerPrefix = "Bearer ";

    private readonly IOptions<LoggingOptions> _options;

    public RequestHeadersEnricher(IOptions<LoggingOptions> options) => _options = options;

    public void Enrich(HttpContext context, IDictionary<string, object?> scope)
    {
        var options = _options.Value;
        if (!options.LogRequestHeaders)
        {
            return;
        }

        var headers = context.Request.Headers;
        if (headers.Count == 0)
        {
            return;
        }

        var masked = options.MaskedHeaders;
        // Use a HashSet for O(1) lookups; comparator is OrdinalIgnoreCase to match HTTP semantics.
        var maskedSet = masked is { Count: > 0 }
            ? new HashSet<string>(masked, StringComparer.OrdinalIgnoreCase)
            : null;

        var projection = new Dictionary<string, string>(headers.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in headers)
        {
            projection[kvp.Key] = MaskValue(kvp.Key, kvp.Value, maskedSet);
        }

        scope[ScopeKey] = projection;
    }

    private static string MaskValue(string name, StringValues value, HashSet<string>? maskedSet)
    {
        if (maskedSet is not null && maskedSet.Contains(name))
        {
            return LoggingDefaults.MaskedValue;
        }

        // Catch tokens leaking through unexpected header names (e.g. custom proxies forwarding
        // Bearer via X-Forwarded-Authorization).
        var raw = value.ToString();
        if (raw.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return LoggingDefaults.MaskedValue;
        }

        return raw;
    }
}

