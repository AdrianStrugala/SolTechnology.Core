using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using SolTechnology.Core.CQRS;

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
        context.Result = new ObjectResult(new Result()
        {
            Error = Error.From(context.Exception),
            IsSuccess = false
        })
        {
            StatusCode = GetStatusCode(context.Exception)

        };
        context.ExceptionHandled = true;
    }

    public int GetStatusCode(Exception exception)
    {
        int code;
        switch (exception)
        {

            case TaskCanceledException:
                code = 499;
                break;
            default:
                code = (int)HttpStatusCode.BadRequest;
                break;
        }
        return code;
    }
}