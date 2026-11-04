﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.API.Filters;

/// <summary>
/// Global MVC exception filter: maps known exception types to RFC 7807 <see cref="ProblemDetails"/>
/// through <see cref="IExceptionStatusCodeMapper"/> and rethrows unmapped exceptions to the host pipeline.
/// Register it with <see cref="ApiCoreMvcOptionsExtensions.AddSolApiCoreFilters"/>.
/// </summary>
/// <remarks>
/// The log level matches <c>SolTechnology.Core.Logging</c>'s request finish log: a mapped 5xx is a server fault
/// (<c>Error</c>, alerts operations), a mapped 4xx is a client fault (<c>Warning</c>, visible but not paging), so
/// validation failures do not flood error alerting. <c>RequestMethod</c> and <c>RequestPath</c> use the same property
/// names as that middleware, and the exception object is logged so providers capture its type, stack trace and inner
/// exceptions.
/// </remarks>
/// <param name="logger">Logger of the filter.</param>
/// <param name="correlationIdService">Source of the correlation id written to <c>Extensions["correlationId"]</c>.</param>
/// <param name="statusCodeMapper">Maps exceptions to HTTP status codes.</param>
/// <param name="options">Controls whether diagnostic exception details are included.</param>
public class ExceptionFilter(
    ILogger<ExceptionFilter> logger,
    ICorrelationIdService correlationIdService,
    IExceptionStatusCodeMapper statusCodeMapper,
    IOptions<ApiExceptionOptions> options) : IExceptionFilter
{

    /// <summary>
    /// Writes a <see cref="ProblemDetails"/> response for a mapped exception and marks it handled;
    /// leaves client aborts and unmapped exceptions unhandled so MVC rethrows them.
    /// </summary>
    public void OnException(ExceptionContext context)
    {
        // Client abort: no body to send; MVC rethrows and the logging middleware logs it as a Warning.
        if (IsClientAbort(context.HttpContext, context.Exception))
        {
            return;
        }

        // Same value as the X-Correlation-Id header and the log scope, so a client can quote it.
        var correlationId = correlationIdService.GetOrGenerate().Value;

        // Mapped exception types — emit RFC 7807 ProblemDetails, set status, mark handled.
        if (statusCodeMapper.TryMap(context.Exception, out var statusCode))
        {
            var level = statusCode >= StatusCodes.Status500InternalServerError
                ? LogLevel.Error
                : LogLevel.Warning;

            logger.Log(
                level,
                context.Exception,
                "Unhandled exception in [{RequestMethod}] [{RequestPath}] → [{StatusCode}]",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                statusCode);

            var problemDetails = ApiProblemDetailsFactory.FromException(
                context.Exception, statusCode, correlationId, options.Value);

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = statusCode,
                ContentTypes = { "application/problem+json" }
            };
            context.ExceptionHandled = true;
            return;
        }

        // Unmapped type is API contract drift: page on-call and let the host pipeline pick the status.
        logger.LogCritical(
            context.Exception,
            "Unmapped exception of type [{ExceptionType}] in [{RequestMethod}] [{RequestPath}] — rethrowing to host pipeline. " +
            "Add an explicit mapping if this exception type is expected.",
            context.Exception.GetType().FullName,
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);
    }


    private static bool IsClientAbort(HttpContext context, Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                return true;
            }
        }
        return false;
    }
}
