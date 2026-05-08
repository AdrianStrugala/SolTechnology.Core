﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.API.Filters;

public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly IExceptionStatusCodeMapper _statusCodeMapper;
    private readonly ApiExceptionOptions _options;

    public ExceptionFilter(
        ILogger<ExceptionFilter> logger,
        ICorrelationIdService correlationIdService,
        IExceptionStatusCodeMapper statusCodeMapper,
        IOptions<ApiExceptionOptions> options)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
        _statusCodeMapper = statusCodeMapper;
        _options = options.Value;
    }

    public void OnException(ExceptionContext context)
    {
        // Client disconnected mid-request — not our incident, no body to send.
        // SolTechnology.Core.Logging.LoggingMiddleware (if registered) will downgrade the
        // finish event to Warning. We leave ExceptionHandled = false so MVC rethrows.
        if (IsClientAbort(context.HttpContext, context.Exception))
        {
            return;
        }

        // Source of truth for the correlation id is Core.Logging's ICorrelationIdService,
        // which already aligned its value with Activity.Current.TraceId (W3C Trace Context)
        // and echoed it on the X-Correlation-Id response header. We surface the same value
        // in ProblemDetails.Extensions["correlationId"] so a client quoting it can be looked
        // up in Seq/AppInsights logs by identical token.
        var correlationId = _correlationIdService.GetOrGenerate().Value;

        // Mapped exception types — emit RFC 7807 ProblemDetails, set status, mark handled.
        if (_statusCodeMapper.TryMap(context.Exception, out var statusCode))
        {
            // Log level coordinated with SolTechnology.Core.Logging.LoggingMiddleware's
            // finish-log: 5xx is a server fault (Error, alerts ops), 4xx is a client fault
            // (Warning, visible but not paging). Without this split, every ValidationException
            // would fire LogError and drown PagerDuty / Sentry / App Insights smart-detection
            // in 400-noise.
            //
            // Property names {RequestMethod}/{RequestPath} are aligned with the same middleware
            // so a single Seq filter catches events from both components. The exception object
            // is logged (not just the message) so structured-logging providers capture
            // exception type, stack trace, inner exceptions and Data.
            var level = statusCode >= StatusCodes.Status500InternalServerError
                ? LogLevel.Error
                : LogLevel.Warning;

            _logger.Log(
                level,
                context.Exception,
                "Unhandled exception in {RequestMethod} {RequestPath} → {StatusCode}",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                statusCode);

            var problemDetails = ApiProblemDetailsFactory.FromException(
                context.Exception, statusCode, correlationId, _options);

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = statusCode,
                ContentTypes = { "application/problem+json" }
            };
            context.ExceptionHandled = true;
            return;
        }

        // Unmapped exception type. Drift in the API contract — page on-call so the type can
        // be added to the mapper. We do NOT pick a default status code: the host pipeline
        // (DeveloperExceptionPage in Development, ASP.NET Core's ProblemDetails-aware
        // UseExceptionHandler in Production) decides. ExceptionHandled stays false → MVC rethrows.
        _logger.LogCritical(
            context.Exception,
            "Unmapped exception of type {ExceptionType} in {RequestMethod} {RequestPath} — rethrowing to host pipeline. " +
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
