namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Failure caused by missing/invalid authentication. Maps to HTTP 401.
/// </summary>
public record UnauthorizedError : Error;
