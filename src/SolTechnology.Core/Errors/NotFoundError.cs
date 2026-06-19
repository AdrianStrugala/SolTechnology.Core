namespace SolTechnology.Core.Errors;

/// <summary>
/// Failure caused by a missing resource. Maps to HTTP 404.
/// </summary>
public record NotFoundError : Error;

