namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Failure caused by a missing resource the caller asked for. The API layer maps this to
/// <c>HTTP 404 Not Found</c>; other transports map analogously (e.g. <c>gRPC NotFound</c>).
/// </summary>
public class NotFoundError : Error
{
}

