namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Failure caused by an authenticated caller lacking the right to perform the operation.
/// The API layer maps this to <c>HTTP 403 Forbidden</c>. Use <see cref="UnauthorizedError"/>
/// when no credentials are present at all.
/// </summary>
public class ForbiddenError : Error
{
}

