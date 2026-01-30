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
        var error = MapExceptionToError(context.Exception);

        logger.LogError(
            context.Exception,
            "Unhandled exception occurred. Message: [{Message}], StatusCode: [{StatusCode}]",
            error.Message,
            error.StatusCode);

        context.Result = new ObjectResult(Result.Fail(error))
        {
            StatusCode = (int)error.StatusCode
        };

        context.ExceptionHandled = true;
    }

    private static Error MapExceptionToError(Exception exception) => exception switch
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

    private static Error HandleValidationException(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g.Select(e => e.ErrorMessage)));

        return new Error
        {
            Message = "One or more validation errors occurred",
            Description = string.Join(Environment.NewLine, errors.Select(e => $"{e.Key}: {e.Value}")),
            StatusCode = HttpStatusCode.BadRequest,
            Recoverable = false
        };
    }

    private static Error HandleArgumentNullException(ArgumentNullException exception) =>
        new()
        {
            Message = $"Required parameter [{exception.ParamName}] is missing",
            Description = exception.Message,
            StatusCode = HttpStatusCode.BadRequest,
            Recoverable = false
        };

    private static Error HandleArgumentException(ArgumentException exception) =>
        new()
        {
            Message = exception.Message,
            Description = exception.ParamName != null ? $"Parameter: {exception.ParamName}" : null,
            StatusCode = HttpStatusCode.BadRequest,
            Recoverable = false
        };

    private static Error HandleUnauthorizedAccessException() =>
        new()
        {
            Message = "Access denied",
            StatusCode = HttpStatusCode.Unauthorized,
            Recoverable = false
        };

    private static Error HandleTaskCanceledException() =>
        new()
        {
            Message = "The client closed the request before the server could respond",
            StatusCode = (HttpStatusCode)499,
            Recoverable = true
        };

    private static Error HandleOperationCanceledException() =>
        new()
        {
            Message = "The request was cancelled or timed out",
            StatusCode = HttpStatusCode.RequestTimeout,
            Recoverable = true
        };

    private static Error HandleKeyNotFoundException(KeyNotFoundException exception) =>
        new()
        {
            Message = exception.Message,
            StatusCode = HttpStatusCode.NotFound,
            Recoverable = false
        };

    private static Error HandleInvalidOperationException(InvalidOperationException exception) =>
        new()
        {
            Message = exception.Message,
            StatusCode = HttpStatusCode.Conflict,
            Recoverable = false
        };

    private static Error HandleUnknownException(Exception exception) =>
        new()
        {
            Message = "An unexpected error occurred. Please try again later.",
#if DEBUG
            Description = exception.StackTrace,
#else
            Description = null,
#endif
            StatusCode = HttpStatusCode.InternalServerError,
            Recoverable = false
        };
}
