using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

/// <summary>
/// MediatR pipeline behavior that automatically tracks every request as a logical operation:
/// <list type="bullet">
///   <item>Logs <c>OperationStarted</c> (EventId 2137) on entry.</item>
///   <item>Logs <c>OperationSucceeded</c> (EventId 2138) with elapsed milliseconds on success.</item>
///   <item>Logs <c>OperationFailed</c> (EventId 2139) with the exception when the handler throws,
///         then rethrows.</item>
///   <item>Opens a logger scope containing every property marked with <see cref="LogScopeAttribute"/>.</item>
///   <item>Starts a child <see cref="Activity"/> on
///         <see cref="CoreLoggingActivitySources.Operations"/> so the operation shows up as a
///         distinct span in OpenTelemetry / Application Insights distributed traces. Tags
///         mirror the scope properties.</item>
/// </list>
/// <para>
/// Zero per-request configuration: every request is tracked, only properties marked with
/// <c>[LogScope]</c> are projected into the scope (PII fields stay out by default).
/// </para>
/// <para>
/// Reflection scan of <typeparamref name="TRequest"/> happens at most once per process — bindings
/// are cached as compiled getters in <see cref="LogScopeBindingCache"/>.
/// </para>
/// <para>
/// When no <see cref="ActivityListener"/> is registered for
/// <see cref="CoreLoggingActivitySources.OperationsName"/>, <see cref="ActivitySource.StartActivity(string, ActivityKind)"/>
/// returns <c>null</c> and the tracing path collapses to a few null-conditional accesses — apps
/// that don't opt into OpenTelemetry pay nothing.
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

        // Activity is null when nobody listens on the source - effectively free in apps without OTel.
        using var activity = CoreLoggingActivitySources.Operations.StartActivity(
            operationName,
            ActivityKind.Internal);

        // Build the scope (and mirror to Activity tags) only when there is something to put in it.
        // The OperationName is already the Activity DisplayName / log message template, no need
        // to duplicate it as a scope property.
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




