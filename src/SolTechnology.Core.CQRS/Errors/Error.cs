using System.Net;

namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Represents an error with structured information for proper error handling and API responses.
/// </summary>
public class Error
{
    /// <summary>
    /// The error message describing what went wrong.
    /// </summary>
    public virtual string Message { get; set; } = null!;

    /// <summary>
    /// Additional description or stack trace information.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether the operation can be retried.
    /// </summary>
    public bool Recoverable { get; set; } = false;

    /// <summary>
    /// The source/origin of the error (e.g., service name, component).
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The HTTP status code associated with this error.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.InternalServerError;

    /// <summary>
    /// Additional contextual details about the error.
    /// </summary>
    public IDictionary<string, object>? Details { get; set; }

    /// <summary>
    /// Creates an Error from an exception.
    /// </summary>
    public static Error From(Exception exception) =>
        new()
        {
            Message = exception.Message,
            Description = exception.StackTrace,
            Source = exception.Source ?? string.Empty,
            StatusCode = HttpStatusCode.InternalServerError,
            Recoverable = false
        };

    /// <summary>
    /// Creates an internal server error.
    /// </summary>
    public static Error Internal(string source, string message, IDictionary<string, object>? details = null) =>
        new()
        {
            Source = source,
            Message = message,
            Recoverable = false,
            StatusCode = HttpStatusCode.InternalServerError,
            Details = details
        };

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string source, string message, IDictionary<string, object>? details = null) =>
        new()
        {
            Source = source,
            Message = message,
            Recoverable = false,
            StatusCode = HttpStatusCode.BadRequest,
            Details = details
        };

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static Error NotFound(string source, string message, IDictionary<string, object>? details = null) =>
        new()
        {
            Source = source,
            Message = message,
            Recoverable = false,
            StatusCode = HttpStatusCode.NotFound,
            Details = details
        };

    /// <summary>
    /// Creates an external service error.
    /// </summary>
    public static Error ExternalService(string source, string message, bool recoverable = true, IDictionary<string, object>? details = null) =>
        new()
        {
            Source = source,
            Message = message,
            Recoverable = recoverable,
            StatusCode = HttpStatusCode.BadGateway,
            Details = details
        };

    /// <summary>
    /// Creates a timeout error.
    /// </summary>
    public static Error Timeout(string source, string message, IDictionary<string, object>? details = null) =>
        new()
        {
            Source = source,
            Message = message,
            Recoverable = true,
            StatusCode = HttpStatusCode.GatewayTimeout,
            Details = details
        };

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    public static Error Unauthorized(string source, string message, IDictionary<string, object>? details = null) =>
        new()
        {
            Source = source,
            Message = message,
            Recoverable = false,
            StatusCode = HttpStatusCode.Unauthorized,
            Details = details
        };
}
