namespace SolTechnology.Core.Errors;

/// <summary>
/// Failure caused by a timeout (query, connection, or external call). Maps to HTTP 408 or 504.
/// </summary>
public record TimeoutError : Error;

