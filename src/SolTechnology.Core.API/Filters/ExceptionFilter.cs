﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using FluentValidation;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.API.Filters;

public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly ApiExceptionOptions _options;

    public ExceptionFilter(
        ILogger<ExceptionFilter> logger,
        ICorrelationIdService correlationIdService,
        IOptions<ApiExceptionOptions> options)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
        _options = options.Value;
    }

    public void OnException(ExceptionContext context)
    {
        // Client disconnected mid-request — not our incident, no envelope to send.
        // SolTechnology.Core.Logging.LoggingMiddleware (if registered) will downgrade the
        // finish event to Warning. We leave ExceptionHandled = false so MVC rethrows.
        if (IsClientAbort(context.HttpContext, context.Exception))
        {
            return;
        }

        // Source of truth for the correlation id is Core.Logging's ICorrelationIdService,
        // which already aligned its value with Activity.Current.TraceId (W3C Trace Context)
        // and echoed it on the X-Correlation-Id response header. We surface the same value
        // in the body so a client quoting it can be looked up in Seq/AppInsights logs by
        // identical token. Fallback to GetOrGenerate ensures non-null even if UseCoreLogging
        // is not in the pipeline (e.g. minimal hosts) — the value still ends up on the
        // log scope via .GetScope() in the structured log call below.
        var correlationId = _correlationIdService.GetOrGenerate().Value;

        // Mapped exception types — wrap in envelope, set status, mark handled.
        if (TryMap(context.Exception, correlationId, _options, out var statusCode, out var error))
        {
            // Log the exception object (not just the message) so structured-logging providers
            // (Serilog/Seq/App Insights) capture exception type, stack trace, inner exceptions
            // and Data. Property names {RequestMethod}/{RequestPath} are aligned with
            // SolTechnology.Core.Logging.LoggingMiddleware so the same Seq filter works
            // regardless of which component emits the event.
            _logger.LogError(
                context.Exception,
                "Unhandled exception in {RequestMethod} {RequestPath} → {StatusCode}",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                statusCode);

            context.Result = new ObjectResult(new Result
            {
                Error = error,
                IsSuccess = false
            })
            {
                StatusCode = statusCode
            };
            context.ExceptionHandled = true;
            return;
        }

        // Unmapped exception type. Drift in the API contract — page on-call so the type can
        // be added to the mapper. We do NOT pick a default status code: the host pipeline
        // (DeveloperExceptionPage in Development, generic 500 in Production, or a custom
        // UseExceptionHandler) decides. ExceptionHandled stays false → MVC rethrows.
        _logger.LogCritical(
            context.Exception,
            "Unmapped exception of type {ExceptionType} in {RequestMethod} {RequestPath} — rethrowing to host pipeline. " +
            "Add an explicit mapping if this exception type is expected.",
            context.Exception.GetType().FullName,
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);
    }

    private static bool TryMap(
        Exception exception,
        string? correlationId,
        ApiExceptionOptions options,
        out int statusCode,
        out Error error)
    {
        switch (exception)
        {
            case ValidationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                // FluentValidation's exception.Message is already user-facing (rule list).
                // ApiErrorFactory will append diagnostic only when IncludeExceptionDetails is on.
                error = ApiErrorFactory.Build(
                    exception,
                    userMessage: "Validation failed",
                    userDescription: exception.Message,
                    correlationId: correlationId,
                    options);
                return true;

            default:
                statusCode = default;
                error = default!;
                return false;
        }
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
