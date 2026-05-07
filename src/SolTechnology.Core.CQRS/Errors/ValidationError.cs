namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Failure caused by invalid request input. Carries a structured per-field map of error
/// messages (<see cref="Errors"/>), mirrored from <c>FluentValidation</c> /
/// <c>System.ComponentModel.DataAnnotations</c> failures.
/// <para>
/// The API layer maps this to <c>HTTP 400 Bad Request</c> with body shaped as
/// <c>Microsoft.AspNetCore.Mvc.ValidationProblemDetails</c> (RFC 7807, <c>errors</c> member).
/// </para>
/// </summary>
public class ValidationError : Error
{
    /// <summary>
    /// Validation failures grouped by property / field name. Empty key (<c>""</c>) is allowed
    /// for object-level rules. Convention: keys use the same casing the wire format requires
    /// (typically camelCase for the request body).
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; init; }
        = new Dictionary<string, string[]>(StringComparer.Ordinal);
}

