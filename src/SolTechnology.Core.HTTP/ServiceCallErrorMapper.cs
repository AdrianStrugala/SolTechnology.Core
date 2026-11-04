using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Polly.Timeout;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.HTTP;

/// <summary>
/// Maps HTTP call failures onto the Core <see cref="SolTechnology.Core.Error"/> taxonomy.
/// Used by the <c>TryXxxAsync</c> methods on <see cref="RequestBuilder"/> to return a
/// <see cref="SolTechnology.Core.Result{T}"/> instead of throwing.
/// </summary>
public static class ServiceCallErrorMapper
{
    private static readonly string[] ProblemMessageProperties = ["title", "message", "detail"];

    /// <summary>
    /// Maps an exception from <see cref="HttpClient.SendAsync"/> to an appropriate Core
    /// <see cref="SolTechnology.Core.Error"/> subtype.
    /// </summary>
    public static SolTechnology.Core.Error FromException(Exception exception) => exception switch
    {
        // Polly per-attempt / outer-budget timeout fired.
        TimeoutRejectedException
            => new TimeoutError
            {
                Message = "Service call timed out",
                Description = exception.Message,
                Recoverable = true
            },

        // TrySend rethrows caller cancellation, so a cancellation here is HttpClient.Timeout.
        OperationCanceledException
            => new TimeoutError
            {
                Message = "Service call timed out",
                Description = exception.Message,
                Recoverable = true
            },

        // Connection failure — socket-level: DNS, connection refused, reset.
        HttpRequestException { InnerException: SocketException } hre
            => new TimeoutError
            {
                Message = "Connection failed",
                Description = hre.Message,
                Recoverable = true
            },

        // Generic HTTP transport failure.
        HttpRequestException hre2
            => new SolTechnology.Core.Error
            {
                Message = "HTTP request failed",
                Description = hre2.Message,
                Recoverable = true
            },

        // Fallback.
        _ => new SolTechnology.Core.Error
        {
            Message = "Unexpected service call error",
            Description = exception.Message,
            Recoverable = false
        }
    };

    /// <summary>
    /// Maps a non-success HTTP status code to an appropriate Core
    /// <see cref="SolTechnology.Core.Error"/> subtype, optionally extracting a message from
    /// a ProblemDetails body.
    /// </summary>
    public static SolTechnology.Core.Error FromStatusCode(
        HttpStatusCode statusCode,
        string? responseBody)
    {
        var extracted = TryExtractProblemMessage(responseBody);
        var message = extracted ?? $"HTTP {(int)statusCode} {statusCode}";
        var recoverable = IsRecoverable(statusCode, responseBody);

        return statusCode switch
        {
            HttpStatusCode.NotFound
                => new NotFoundError { Message = message, Recoverable = recoverable },

            HttpStatusCode.Conflict
                => new ConflictError { Message = message, Recoverable = recoverable },

            HttpStatusCode.Unauthorized
                => new UnauthorizedError { Message = message, Recoverable = recoverable },

            HttpStatusCode.Forbidden
                => new ForbiddenError { Message = message, Recoverable = recoverable },

            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity
                => new ValidationError
                {
                    Message = message,
                    Recoverable = recoverable,
                    Errors = new Dictionary<string, string[]>()
                },

            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout
                => new TimeoutError { Message = message, Recoverable = true },

            // 5xx — transient by default.
            >= HttpStatusCode.InternalServerError
                => new SolTechnology.Core.Error { Message = message, Recoverable = recoverable },

            // Everything else — generic error.
            _ => new SolTechnology.Core.Error { Message = message, Recoverable = recoverable }
        };
    }

    /// <summary>
    /// Maps a deserialization failure to a Core error.
    /// </summary>
    public static SolTechnology.Core.Error FromDeserializationFailure(Exception exception, string? body)
        => new SolTechnology.Core.Error
        {
            Message = "Response deserialization failed",
            Description = exception.Message,
            Recoverable = false
        };

    private static string? TryExtractProblemMessage(string? body)
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

    private static bool IsRecoverable(HttpStatusCode statusCode, string? body)
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

