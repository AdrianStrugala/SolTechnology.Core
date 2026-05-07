using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace SolTechnology.Core.API.Exceptions;

/// <summary>
/// Default exception → HTTP status mapping. Inherit and override <see cref="TryMap"/> to add
/// project-specific exception types; call <c>base.TryMap</c> to keep the framework defaults.
/// <para>
/// <strong>Deliberately small.</strong> Each entry has a clear, transport-agnostic justification.
/// Cases where the meaning is genuinely ambiguous (e.g. <see cref="OperationCanceledException"/>
/// outside a request abort, <see cref="TimeoutException"/> in business code) are <em>not</em>
/// mapped here on purpose — they fall through to the A+E policy in the filter (LogCritical +
/// rethrow), which is what we want for "we have not decided what this means yet" cases.
/// </para>
/// </summary>
public class DefaultExceptionStatusCodeMapper : IExceptionStatusCodeMapper
{
    /// <inheritdoc />
    public virtual bool TryMap(Exception exception, out int statusCode)
    {
        switch (exception)
        {
            // FluentValidation. Body becomes ValidationProblemDetails (per-field errors)
            // through ApiProblemDetailsFactory.FromException's special-case branch.
            case ValidationException:
                statusCode = StatusCodes.Status400BadRequest;
                return true;

            // Caller passed an argument the method considers invalid. Could come from a guard
            // in domain code; treat as bad input. Catches ArgumentNullException too (subtype).
            case ArgumentException:
                statusCode = StatusCodes.Status400BadRequest;
                return true;

            // .NET convention: caller is not authorized to perform the operation. Despite the
            // 401 mnemonic in the type name, the modern guidance is 403 (the caller has been
            // identified and the operation is forbidden). 401 means "no credentials"; the auth
            // pipeline emits that on its own before MVC ever runs. Mapping this to 403 here
            // matches both the semantic intent of System.UnauthorizedAccessException and the
            // RFC 7235 distinction.
            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status403Forbidden;
                return true;

            // BCL "key not in collection". Domain code that throws this for "entity not in
            // store" gets a free 404; teams that prefer an explicit NotFoundError on the
            // Result path are unaffected.
            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                return true;

            // Reached an endpoint backed by an unimplemented feature. 501 is the only HTTP
            // status that semantically matches "the server does not support the functionality
            // required to fulfill the request" (RFC 9110 §15.6.2).
            case NotImplementedException:
                statusCode = StatusCodes.Status501NotImplemented;
                return true;

            default:
                statusCode = default;
                return false;
        }
    }
}

