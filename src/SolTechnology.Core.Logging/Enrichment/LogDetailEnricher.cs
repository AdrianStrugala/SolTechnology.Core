using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace SolTechnology.Core.Logging.Enrichment;

/// <summary>
/// Aggregates every <c>LogDetail(...)</c> registration into a single <see cref="ILogScopeEnricher"/>
/// instance. Reads from headers / URL synchronously and from the parsed JSON body cached on
/// <see cref="HttpContext.Items"/> by the request-logging middleware.
/// </summary>
internal sealed class LogDetailEnricher : ILogScopeEnricher, IRequiresRequestBody
{
    /// <summary>Key under which <c>LoggingMiddleware</c> caches the parsed JSON body.</summary>
    internal const string ParsedBodyItemKey = "SolTechnology.Core.Logging.ParsedBody";

    private readonly LogDetailDescriptor[] _descriptors;

    public LogDetailEnricher(IEnumerable<LogDetailDescriptor> descriptors)
    {
        _descriptors = descriptors.ToArray();
    }

    public bool RequiresBody(HttpContext context)
    {
        foreach (var descriptor in _descriptors)
        {
            if (descriptor.Source == LogDetailSource.Body && MatchesEndpoint(context, descriptor))
            {
                return true;
            }
        }
        return false;
    }

    public void Enrich(HttpContext context, IDictionary<string, object?> scope)
    {
        foreach (var descriptor in _descriptors)
        {
            if (!MatchesEndpoint(context, descriptor))
            {
                continue;
            }

            var value = descriptor.Source switch
            {
                LogDetailSource.Header => ReadHeader(context, descriptor.PropertyName),
                LogDetailSource.Url => ReadUrl(context, descriptor.PropertyName),
                LogDetailSource.Body => ReadBody(context, descriptor.PropertyName),
                _ => null,
            };

            if (!string.IsNullOrEmpty(value))
            {
                scope[descriptor.ScopeName] = value;
            }
        }
    }

    private static bool MatchesEndpoint(HttpContext context, LogDetailDescriptor descriptor)
    {
        if (descriptor.Endpoints is null || descriptor.Endpoints.Count == 0)
        {
            return true;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        foreach (var prefix in descriptor.Endpoints)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string? ReadHeader(HttpContext context, string name)
        => context.Request.Headers.TryGetValue(name, out var values) && values.Count > 0
            ? values.ToString()
            : null;

    private static string? ReadUrl(HttpContext context, string name)
    {
        if (context.Request.Query.TryGetValue(name, out var query) && query.Count > 0)
        {
            return query.ToString();
        }

        if (context.Request.RouteValues.TryGetValue(name, out var route) && route is not null)
        {
            return route.ToString();
        }

        return null;
    }

    private static string? ReadBody(HttpContext context, string name)
    {
        if (!context.Items.TryGetValue(ParsedBodyItemKey, out var raw) || raw is not JsonDocument document)
        {
            return null;
        }

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // Match exactly first; then try a PascalCase fallback so apps can declare 'name' and
        // still pick up DTOs serialised with PascalCase property names.
        if (TryGetScalarString(root, name, out var value))
        {
            return value;
        }

        if (name.Length > 0 && char.IsLower(name[0]))
        {
            var pascal = char.ToUpperInvariant(name[0]) + name[1..];
            if (TryGetScalarString(root, pascal, out value))
            {
                return value;
            }
        }
        else if (name.Length > 0 && char.IsUpper(name[0]))
        {
            var camel = char.ToLowerInvariant(name[0]) + name[1..];
            if (TryGetScalarString(root, camel, out value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool TryGetScalarString(JsonElement element, string propertyName, out string? value)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            value = prop.ValueKind switch
            {
                JsonValueKind.String => prop.GetString(),
                JsonValueKind.Number => prop.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null,
            };
            return !string.IsNullOrEmpty(value);
        }
        value = null;
        return false;
    }
}

