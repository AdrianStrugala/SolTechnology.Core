﻿using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Masking;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

/// <summary>
/// Pipeline behavior that tracks every request as a logical operation with structured logging
/// and optional OpenTelemetry Activity spans.
/// </summary>
/// <param name="logger">The logger the operation entries are written to.</param>
public sealed class LoggingPipelineBehavior<TRequest, TResponse>(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Logs the start and outcome of the request inside a <c>[LogScope]</c> scope and an activity: a
    /// success, a failed <see cref="Result"/> (error type and message only), or an exception.
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var operationName = typeof(TRequest).Name;
        var bindings = LogScopeBindingCache.GetBindings(typeof(TRequest));

        using var activity = OperationsActivitySource.Instance.StartActivity(
            operationName,
            ActivityKind.Internal);

        IDisposable? scope = null;
        if (bindings.Length > 0)
        {
            var scopeProperties = new Dictionary<string, object?>(bindings.Length);
            foreach (var binding in bindings)
            {
                var value = binding.Getter(request);
                var scopeValue = binding.Masking is not null
                    ? PiiMask.Apply(value?.ToString(), binding.Masking.Mode, binding.Masking.KeepChars)
                    : value;
                scopeProperties[binding.Key] = scopeValue;
                activity?.SetTag(binding.Key, scopeValue);
            }
            scope = logger.BeginScope(scopeProperties);
        }

        using (scope)
        {
            var sw = Stopwatch.StartNew();
            logger.OperationStarted(operationName);

            try
            {
                var result = await next();
                if (result is Result { IsFailure: true } failed)
                {
                    OperationResultLogMessages.OperationFailedWithResult(
                        logger,
                        operationName,
                        sw.ElapsedMilliseconds,
                        failed.Error?.GetType().Name ?? nameof(Error),
                        failed.Error?.Message);
                    activity?.SetStatus(ActivityStatusCode.Error);
                    return result;
                }

                logger.OperationSucceeded(operationName, sw.ElapsedMilliseconds);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception e)
            {
                logger.OperationFailed(operationName, sw.ElapsedMilliseconds, e);
                activity?.SetStatus(ActivityStatusCode.Error, e.Message);
                activity?.AddException(e);
                throw;
            }
        }
    }
}
