using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Hangfire.Filters;

/// <summary>
/// Propagates correlation id across the Hangfire enqueue→execute boundary.
/// On enqueue: saves the current correlation id as a job parameter.
/// On execute: restores it into <see cref="ICorrelationIdService"/> and pushes a log scope.
/// </summary>
internal sealed class CorrelationIdJobFilter(
    ICorrelationIdService correlationIdService,
    ILogger<CorrelationIdJobFilter> logger) : JobFilterAttribute, IClientFilter, IServerFilter
{
    private const string LogScopeKey = "CorrelationIdLogScope";

    public void OnCreating(CreatingContext filterContext)
    {
        var correlationId = correlationIdService.GetOrGenerate();
        filterContext.SetJobParameter(CorrelationId.ScopeKey, correlationId.Value);
    }

    public void OnCreated(CreatedContext filterContext)
    {
    }

    public void OnPerforming(PerformingContext filterContext)
    {
        var storedValue = filterContext.GetJobParameter<string>(CorrelationId.ScopeKey);

        CorrelationId correlationId;
        if (!string.IsNullOrEmpty(storedValue))
        {
            correlationId = CorrelationId.FromString(storedValue);
            correlationIdService.Set(correlationId);
        }
        else
        {
            correlationId = correlationIdService.GetOrGenerate();
        }

        var scope = logger.BeginScope(correlationId.GetScope());
        filterContext.Items[LogScopeKey] = scope;
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        if (filterContext.Items.TryGetValue(LogScopeKey, out var scopeObj) && scopeObj is IDisposable scope)
        {
            scope.Dispose();
        }
    }
}


