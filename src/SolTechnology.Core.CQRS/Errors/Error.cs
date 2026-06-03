#nullable enable
using System.Text.Json.Serialization;

namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Base error surfaced by the application layer through <see cref="Result"/> /
/// <see cref="Result{T}"/>. Use a subtype (<see cref="NotFoundError"/>, <see cref="ConflictError"/>,
/// <see cref="ValidationError"/>, <see cref="UnauthorizedError"/>, <see cref="ForbiddenError"/>)
/// when the failure has well-known semantics that downstream layers can map deterministically.
/// </summary>
[JsonDerivedType(typeof(Error), "error")]
[JsonDerivedType(typeof(NotFoundError), "notFound")]
[JsonDerivedType(typeof(ConflictError), "conflict")]
[JsonDerivedType(typeof(ValidationError), "validation")]
[JsonDerivedType(typeof(UnauthorizedError), "unauthorized")]
[JsonDerivedType(typeof(ForbiddenError), "forbidden")]
[JsonDerivedType(typeof(AggregateError), "aggregate")]
public record Error
{
    public virtual string Message { get; init; } = null!;
    public string? Description { get; init; }
    public bool Recoverable { get; init; }

    /// <summary>
    /// Optional correlation identifier echoed to the client for support tickets.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Creates an <see cref="Error"/> from an exception. Does not expose stack trace.
    /// </summary>
    public static Error From(Exception exception) =>
        new()
        {
            Message = exception.Message
        };
}
