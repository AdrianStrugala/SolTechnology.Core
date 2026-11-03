#nullable enable
using System.Text.Json.Serialization;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core;

/// <summary>
/// Base error surfaced by the application layer through <see cref="Result"/> /
/// <see cref="Result{T}"/>. Use a subtype (<see cref="NotFoundError"/>, <see cref="ConflictError"/>,
/// <see cref="ValidationError"/>, <see cref="UnauthorizedError"/>, <see cref="ForbiddenError"/>)
/// when the failure has well-known semantics that downstream layers can map deterministically.
/// Subtypes without a registered discriminator serialize as their nearest registered ancestor.
/// </summary>
[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(Error), "error")]
[JsonDerivedType(typeof(NotFoundError), "notFound")]
[JsonDerivedType(typeof(ConflictError), "conflict")]
[JsonDerivedType(typeof(ValidationError), "validation")]
[JsonDerivedType(typeof(UnauthorizedError), "unauthorized")]
[JsonDerivedType(typeof(ForbiddenError), "forbidden")]
[JsonDerivedType(typeof(AggregateError), "aggregate")]
[JsonDerivedType(typeof(TimeoutError), "timeout")]
[JsonDerivedType(typeof(DeadlockError), "deadlock")]
public record Error
{
    /// <summary>Human-readable description of what went wrong.</summary>
    public virtual string Message { get; init; } = null!;

    /// <summary>Optional additional detail about the failure.</summary>
    public string? Description { get; init; }

    /// <summary>True when retrying the operation may succeed (for example a timeout or a deadlock).</summary>
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

