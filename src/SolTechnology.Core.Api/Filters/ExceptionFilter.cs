using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;

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
        context.Result = new ObjectResult(new ResponseEnvelope
        {
            Error = context.Exception.Message,
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