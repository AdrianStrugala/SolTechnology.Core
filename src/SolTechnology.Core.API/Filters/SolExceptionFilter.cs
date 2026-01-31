using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentValidation;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using System.Net;

namespace SolTechnology.Core.API.Filters;

/// <summary>
/// Global exception filter that catches unhandled exceptions and converts them to
/// Result with Error. The ResponseEnvelopeFilter will then convert the Error to ProblemDetails.
/// </summary>
public class SolExceptionFilter(ILogger<SolExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (error, statusCode) = MapExceptionToError(context.Exception);

        logger.LogError(
            context.Exception,
            "Unhandled exception occurred. Message: [{Message}], StatusCode: [{StatusCode}]",
            error.Message,
            statusCode);

        context.Result = new ObjectResult(Result.Fail(error))
        {
            StatusCode = (int)statusCode
        };

        context.ExceptionHandled = true;
    }

    private static (Error Error, HttpStatusCode StatusCode) MapExceptionToError(Exception exception) => exception switch
    {
        ValidationException validationException => HandleValidationException(validationException),
        ArgumentNullException argumentNullException => HandleArgumentNullException(argumentNullException),
        ArgumentException argumentException => HandleArgumentException(argumentException),
        UnauthorizedAccessException => HandleUnauthorizedAccessException(),
        TaskCanceledException => HandleTaskCanceledException(),
        OperationCanceledException => HandleOperationCanceledException(),
        KeyNotFoundException keyNotFoundException => HandleKeyNotFoundException(keyNotFoundException),
        InvalidOperationException invalidOperationException => HandleInvalidOperationException(invalidOperationException),
        _ => HandleUnknownException(exception)
    };

    private static (Error Error, HttpStatusCode StatusCode) HandleValidationException(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g.Select(e => e.ErrorMessage)));

        return (new Error
        {
            Message = "One or more validation errors occurred",
            Description = string.Join(Environment.NewLine, errors.Select(e => $"{e.Key}: {e.Value}")),
            Recoverable = false
        }, HttpStatusCode.BadRequest);
    }

    private static (Error Error, HttpStatusCode StatusCode) HandleArgumentNullException(ArgumentNullException exception) =>
        (new Error
        {
            Message = $"Required parameter [{exception.ParamName}] is missing",
            Description = exception.Message,
            Recoverable = false
        }, HttpStatusCode.BadRequest);

    private static (Error Error, HttpStatusCode StatusCode) HandleArgumentException(ArgumentException exception) =>
        (new Error
        {
            Message = exception.Message,
            Description = exception.ParamName != null ? $"Parameter: {exception.ParamName}" : null,
            Recoverable = false
        }, HttpStatusCode.BadRequest);

    private static (Error Error, HttpStatusCode StatusCode) HandleUnauthorizedAccessException() =>
        (new Error
        {
            Message = "Access denied",
            Recoverable = false
        }, HttpStatusCode.Unauthorized);

    private static (Error Error, HttpStatusCode StatusCode) HandleTaskCanceledException() =>
        (new Error
        {
            Message = "The client closed the request before the server could respond",
            Recoverable = true
        }, (HttpStatusCode)499);

    private static (Error Error, HttpStatusCode StatusCode) HandleOperationCanceledException() =>
        (new Error
        {
            Message = "The request was cancelled or timed out",
            Recoverable = true
        }, HttpStatusCode.RequestTimeout);

    private static (Error Error, HttpStatusCode StatusCode) HandleKeyNotFoundException(KeyNotFoundException exception) =>
        (new Error
        {
            Message = exception.Message,
            Recoverable = false
        }, HttpStatusCode.NotFound);

    private static (Error Error, HttpStatusCode StatusCode) HandleInvalidOperationException(InvalidOperationException exception) =>
        (new Error
        {
            Message = exception.Message,
            Recoverable = false
        }, HttpStatusCode.Conflict);

    private static (Error Error, HttpStatusCode StatusCode) HandleUnknownException(Exception exception) =>
        (new Error
        {
            Message = "An unexpected error occurred. Please try again later.",
#if DEBUG
            Description = exception.StackTrace,
#else
            Description = null,
#endif
            Recoverable = false
        }, HttpStatusCode.InternalServerError);
}
