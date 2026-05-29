using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

/// <summary>
/// Pipeline behavior that tracks every request as a logical operation with structured logging
/// and optional OpenTelemetry Activity spans.
/// </summary>
public sealed class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger;

    public LoggingPipelineBehavior(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var operationName = typeof(TRequest).Name;
        var bindings = LogScopeBindingCache.GetBindings(typeof(TRequest));

        using var activity = CoreLoggingActivitySources.Operations.StartActivity(
            operationName,
            ActivityKind.Internal);

        IDisposable? scope = null;
        if (bindings.Length > 0)
        {
            var scopeProperties = new Dictionary<string, object?>(bindings.Length);
            foreach (var binding in bindings)
            {
                var value = binding.Getter(request);
                scopeProperties[binding.Key] = value;
                activity?.SetTag(binding.Key, value);
            }
            scope = _logger.BeginScope(scopeProperties);
        }

        using (scope)
        {
            var sw = Stopwatch.StartNew();
            _logger.OperationStarted(operationName);

            try
            {
                var result = await next();
                _logger.OperationSucceeded(operationName, sw.ElapsedMilliseconds);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception e)
            {
                _logger.OperationFailed(operationName, sw.ElapsedMilliseconds, e);
                activity?.SetStatus(ActivityStatusCode.Error, e.Message);
                activity?.AddException(e);
                throw;
            }
        }
    }
}
