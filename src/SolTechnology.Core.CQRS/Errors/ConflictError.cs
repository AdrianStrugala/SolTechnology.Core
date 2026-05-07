namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Failure caused by a state collision (e.g. duplicate key, optimistic-concurrency mismatch,
/// resource-already-exists). The API layer maps this to <c>HTTP 409 Conflict</c>.
/// </summary>
public class ConflictError : Error
{
}

