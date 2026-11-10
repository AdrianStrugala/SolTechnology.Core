﻿using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Masking;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

/// <summary>
/// Pipeline behavior that tracks every request as a logical operation with structured logging
/// and a Chronicle step: a child of the current step inside a root (an HTTP request, a job), or
/// its own root with a 2154 report otherwise. The step also produces the operation's activity.
/// </summary>
/// <param name="logger">The logger the operation entries are written to.</param>
/// <param name="chronicle">The Chronicle that records the operation step.</param>
public sealed class LoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger,
    IChronicle chronicle)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Logs the start and outcome of the request inside a <c>[LogScope]</c> scope and a Chronicle
    /// step: a success, a failed <see cref="Result"/> (error type and message only), a caller
    /// cancellation, or an exception.
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var operationName = typeof(TRequest).Name;
        var bindings = LogScopeBindingCache.GetBindings(typeof(TRequest));

        using var step = chronicle.Begin(operationName);

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
                step.Set(binding.Key, scopeValue);
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
                    var errorType = failed.Error?.GetType().Name ?? nameof(Error);
                    OperationResultLogMessages.OperationFailedWithResult(
                        logger,
                        operationName,
                        sw.ElapsedMilliseconds,
                        errorType,
                        failed.Error?.Message);
                    step.Fail(errorType, failed.Error?.Message);
                    return result;
                }

                logger.OperationSucceeded(operationName, sw.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException e) when (cancellationToken.IsCancellationRequested)
            {
                logger.OperationFailed(operationName, sw.ElapsedMilliseconds, e);
                step.Cancel();
                throw;
            }
            catch (Exception e)
            {
                logger.OperationFailed(operationName, sw.ElapsedMilliseconds, e);
                step.Fail(e);
                throw;
            }
        }
    }
}
