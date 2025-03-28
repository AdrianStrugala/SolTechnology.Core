using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using FluentValidation;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.Api.Filters;

public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception.Message);

        var (code, error) = HandleException(context.Exception);

        context.Result = new ObjectResult(new Result()
        {
            Error = error,
            IsSuccess = false
        })
        {
            StatusCode = code

        };
        context.ExceptionHandled = true;
    }

    public (int, Error) HandleException(Exception exception)
    {
        var error = Error.From(exception);
        int code = (int)HttpStatusCode.BadRequest;
        switch (exception)
        {
            case TaskCanceledException:
                code = 499;
                break;

            case ValidationException:
                error = new Error
                {
                    Message = "Validation failed",
                    Description = exception.Message
                };
                break;
        }
        return (code, error);
    }
}