﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.API.Middlewares;

//FILTER is preferred
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(
        HttpContext context,
        ILogger<ExceptionHandlerMiddleware> logger,
        ICorrelationIdService correlationIdService,
        IOptions<ApiExceptionOptions> options)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (IsClientAbort(context, exception))
        {
            // Client disconnected. SolTechnology.Core.Logging.LoggingMiddleware (if registered)
            // will downgrade the finish event to Warning. Don't log here, don't write a body —
            // the connection is gone.
            throw;
        }
        catch (Exception exception)
        {
            if (!TryMap(exception, out var statusCode))
            {
                // Unmapped exception type. Drift in the API contract — page on-call so the
                // type can be added to the mapper. We do NOT pick a default status code:
                // the host pipeline (DeveloperExceptionPage in Development, generic 500 in
                // Production, or a custom UseExceptionHandler) decides.
                logger.LogCritical(
                    exception,
                    "Unmapped exception of type {ExceptionType} in {RequestMethod} {RequestPath} — rethrowing to host pipeline. " +
                    "Add an explicit mapping if this exception type is expected.",
                    exception.GetType().FullName,
                    context.Request.Method,
                    context.Request.Path);
                throw;
            }

            // Log the exception object (not just the message) so structured-logging providers
            // capture exception type, stack trace, inner exceptions and Data. Property names
            // {RequestMethod}/{RequestPath} are aligned with SolTechnology.Core.Logging.LoggingMiddleware.
            logger.LogError(
                exception,
                "Unhandled exception in {RequestMethod} {RequestPath} → {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                statusCode);

            var response = context.Response;
            response.StatusCode = statusCode;

            // Correlation id sourced from Core.Logging — same value as on the X-Correlation-Id
            // response header and the CorrelationId log scope property.
            var correlationId = correlationIdService.GetOrGenerate().Value;

            // ApiErrorFactory replaces Error.From(exception): the latter unconditionally puts
            // exception.StackTrace into Description (CWE-209). The factory only adds diagnostic
            // detail when ApiExceptionOptions.IncludeExceptionDetails is explicitly enabled.
            var responseEnvelope = new Result
            {
                Error = ApiErrorFactory.Build(
                    exception,
                    userMessage: "An unexpected error occurred.",
                    userDescription: null,
                    correlationId: correlationId,
                    options.Value),
                IsSuccess = false
            };

            await response.WriteAsJsonAsync(responseEnvelope);
        }
    }

    private static bool TryMap(Exception exception, out int statusCode)
    {
        switch (exception)
        {
            // Note: ExceptionFilter additionally maps ValidationException since FluentValidation
            // errors only originate from the MVC pipeline. This middleware intentionally does
            // not mirror that — it's a non-MVC safety net (auth, routing, other middleware).
            // Cross-cutting types are added in the future shared IExceptionStatusCodeMapper.
            default:
                statusCode = default;
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
