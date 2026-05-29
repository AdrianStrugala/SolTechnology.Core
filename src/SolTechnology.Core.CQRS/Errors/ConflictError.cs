namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Failure caused by a state collision (duplicate key, optimistic concurrency). Maps to HTTP 409.
/// </summary>
public record ConflictError : Error;
