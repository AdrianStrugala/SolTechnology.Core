using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Hangfire.Filters;

/// <summary>
/// Propagates correlation id across the Hangfire enqueue→execute boundary.
/// On enqueue: saves the current correlation id (or a new one) as a job parameter without touching ambient state.
/// On execute: restores it into <see cref="ICorrelationIdService"/> and pushes a log scope; after the run the
/// worker thread's previous ambient id is restored, so it never leaks into the next job.
/// </summary>
internal sealed class CorrelationIdJobFilter(
    ICorrelationIdService correlationIdService,
    ILogger<CorrelationIdJobFilter> logger) : JobFilterAttribute, IClientFilter, IServerFilter
{
    private const string LogScopeKey = "CorrelationIdLogScope";
    private const string PreviousCorrelationIdKey = "PreviousCorrelationId";

    public void OnCreating(CreatingContext filterContext)
    {
        var correlationId = correlationIdService.Get() ?? CorrelationId.Generate();
        filterContext.SetJobParameter(CorrelationId.ScopeKey, correlationId.Value);
    }

    public void OnCreated(CreatedContext filterContext)
    {
    }

    public void OnPerforming(PerformingContext filterContext)
    {
        var storedValue = filterContext.GetJobParameter<string>(CorrelationId.ScopeKey);
        var correlationId = string.IsNullOrEmpty(storedValue)
            ? CorrelationId.Generate()
            : CorrelationId.FromString(storedValue);

        filterContext.Items[PreviousCorrelationIdKey] = correlationIdService.Get();
        correlationIdService.Set(correlationId);
        filterContext.Items[LogScopeKey] = logger.BeginScope(correlationId.GetScope());
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        if (filterContext.Items.TryGetValue(LogScopeKey, out var scopeObj) && scopeObj is IDisposable scope)
        {
            scope.Dispose();
        }

        if (!filterContext.Items.TryGetValue(PreviousCorrelationIdKey, out var previous))
        {
            return;
        }

        if (previous is CorrelationId previousCorrelationId)
        {
            correlationIdService.Set(previousCorrelationId);
        }
        else
        {
            correlationIdService.Clear();
        }
    }
}
