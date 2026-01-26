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
    /// The HTTP status code associated with this error.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.InternalServerError;

    /// <summary>
    /// Creates an Error from an exception.
    /// </summary>
    public static Error From(Exception exception) =>
        new()
        {
            Message = exception.Message,
            Description = exception.StackTrace,
            StatusCode = HttpStatusCode.InternalServerError,
            Recoverable = false
        };
}
