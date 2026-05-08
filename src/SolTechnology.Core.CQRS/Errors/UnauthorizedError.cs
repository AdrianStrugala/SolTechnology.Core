namespace SolTechnology.Core.CQRS.Errors;

/// <summary>
/// Failure caused by missing / invalid authentication. The API layer maps this to
/// <c>HTTP 401 Unauthorized</c>. Use <see cref="ForbiddenError"/> when the caller is
/// authenticated but lacks permission.
/// </summary>
public class UnauthorizedError : Error
{
}

