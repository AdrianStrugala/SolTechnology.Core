namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Failure caused by insufficient permissions. Maps to HTTP 403.
/// </summary>
public record ForbiddenError : Error;
