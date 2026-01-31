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
    /// Creates an Error from an exception.
    /// </summary>
    public static Error From(Exception exception) =>
        new()
        {
            Message = exception.Message,
            Description = exception.StackTrace,
            Recoverable = false
        };
}
