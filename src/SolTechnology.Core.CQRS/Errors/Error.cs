﻿namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Generic failure surfaced by the application layer through <c>Result</c> /
/// <c>Result&lt;T&gt;</c>. Use a subtype (<see cref="NotFoundError"/>, <see cref="ConflictError"/>,
/// <see cref="ValidationError"/>, <see cref="UnauthorizedError"/>, <see cref="ForbiddenError"/>)
/// when the failure has a well-known semantic that downstream layers (HTTP, gRPC, message bus)
/// can map deterministically.
/// <para>
/// Subtype-based modeling keeps <c>Core.CQRS</c> free of transport concerns: handlers state
/// "resource not found" via <see cref="NotFoundError"/>; the API layer turns it into
/// <c>404 Not Found</c>; a hypothetical gRPC layer turns it into <c>StatusCode.NotFound</c>.
/// </para>
/// </summary>
public class Error
{
    public virtual string Message { get; set; } = null!;
    public string? Description { get; set; }
    public bool Recoverable { get; set; } = false;

    /// <summary>
    /// Optional correlation identifier echoed to the client so they can quote it in support tickets.
    /// Populated by API-layer exception handlers from <c>ICorrelationIdService</c> /
    /// <c>Activity.Current.TraceId</c>; matches the <c>X-Correlation-Id</c> response header and
    /// the <c>CorrelationId</c> property in the log scope.
    /// </summary>
    public string? CorrelationId { get; set; }

    public static Error From(Exception exception) =>
        new()
        {
            Message = exception.Message,
            Description = exception.StackTrace
        };
}
