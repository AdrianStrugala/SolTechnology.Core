﻿namespace SolTechnology.Core.CQRS.Errors;

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
