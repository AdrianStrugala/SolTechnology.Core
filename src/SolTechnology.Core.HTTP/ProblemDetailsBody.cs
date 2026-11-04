using System.Net;
using System.Text.Json;

namespace SolTechnology.Core.HTTP;

// Reads the ProblemDetails fields ServiceCallErrorMapper uses from an error response body.
internal static class ProblemDetailsBody
{
    private static readonly string[] ProblemMessageProperties = ["title", "message", "detail"];

    public static string? TryExtractMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var name in ProblemMessageProperties)
            {
                if (doc.RootElement.TryGetProperty(name, out var property))
                {
                    return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON: the status code alone describes the failure.
        }

        return null;
    }

    public static bool IsRecoverable(HttpStatusCode statusCode, string? body)
    {
        // Explicit signal from ProblemDetails body takes precedence.
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("recoverable", out var prop) &&
                    prop.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    return prop.GetBoolean();
                }
            }
            catch (JsonException)
            {
                // Not JSON: fall through to the status heuristic.
            }
        }

        // Heuristic: 5xx = transient/recoverable; 4xx = deterministic/not recoverable.
        return (int)statusCode >= 500;
    }
}
