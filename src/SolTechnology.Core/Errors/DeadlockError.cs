namespace SolTechnology.Core.Errors;

/// <summary>
/// Failure caused by a SQL deadlock (error 1205). Caller may retry. Maps to HTTP 409 or 503.
/// </summary>
public record DeadlockError : Error;

