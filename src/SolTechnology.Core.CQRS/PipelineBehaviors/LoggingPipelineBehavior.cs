using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

/// <summary>
/// MediatR pipeline behavior that automatically tracks every request as a logical operation:
/// <list type="bullet">
///   <item>Logs <c>OperationStarted</c> (EventId 2137) on entry.</item>
///   <item>Logs <c>OperationSucceeded</c> (EventId 2138) with elapsed milliseconds on success.</item>
///   <item>Logs <c>OperationFailed</c> (EventId 2139) with the exception when the handler throws,
///         then rethrows.</item>
///   <item>Opens a logger scope containing every property marked with <see cref="LogScopeAttribute"/>.</item>
/// </list>
/// <para>
/// Zero per-request configuration: every request is tracked, only properties marked with
/// <c>[LogScope]</c> are projected into the scope (PII fields stay out by default).
/// </para>
/// <para>
/// Reflection scan of <typeparamref name="TRequest"/> happens at most once per process — bindings
/// are cached as compiled getters in <see cref="LogScopeBindingCache"/>.
/// </para>
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

        // Build the scope only when there is something to put in it. The OperationName itself
        // is part of the structured log template (no need to duplicate it as a scope property).
        IDisposable? scope = null;
        if (bindings.Length > 0)
        {
            var scopeProperties = new Dictionary<string, object?>(bindings.Length);
            foreach (var binding in bindings)
            {
                scopeProperties[binding.Key] = binding.Getter(request);
            }
            scope = _logger.BeginScope(scopeProperties);
        }

        // 'using' guarantees the scope (when present) is popped from MEL's AsyncLocal stack
        // even when next() throws.
        using (scope)
        {
            var sw = Stopwatch.StartNew();
            _logger.OperationStarted(operationName);

            try
            {
                var result = await next();
                _logger.OperationSucceeded(operationName, sw.ElapsedMilliseconds);
                return result;
            }
            catch (Exception e)
            {
                _logger.OperationFailed(operationName, sw.ElapsedMilliseconds, e);
                throw;
            }
        }
    }
}


