using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Infrastructure.Hangfire;

/// <summary>
/// Hangfire filter that propagates correlation IDs through background jobs.
/// Ensures the same correlation ID is used across the entire request lifecycle.
/// </summary>
public class HangfireCorrelationIdFilter : JobFilterAttribute, IClientFilter, IServerFilter
{
    public const string CorrelationIdKey = "CorrelationId";

    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();
    private readonly ILogger<HangfireCorrelationIdFilter>? _logger;

    public HangfireCorrelationIdFilter(ILogger<HangfireCorrelationIdFilter>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the current correlation ID for the executing async context.
    /// </summary>
    public static string? GetCurrentCorrelationId() => CurrentCorrelationId.Value;

    /// <summary>
    /// Sets the current correlation ID for the executing async context.
    /// </summary>
    public static void SetCurrentCorrelationId(string? correlationId) => CurrentCorrelationId.Value = correlationId;

    public void OnCreating(CreatingContext context)
    {
        var correlationId = CurrentCorrelationId.Value ?? Guid.NewGuid().ToString("N");
        context.SetJobParameter(CorrelationIdKey, correlationId);

        _logger?.LogDebug(
            "Creating job with CorrelationId: [{CorrelationId}]",
            correlationId);
    }

    public void OnCreated(CreatedContext context)
    {
        // CorrelationId was set in OnCreating, just log the job creation
        _logger?.LogDebug(
            "Job [{JobId}] created with CorrelationId: [{CorrelationId}]",
            context.BackgroundJob?.Id,
            CurrentCorrelationId.Value);
    }

    public void OnPerforming(PerformingContext context)
    {
        var correlationId = context.GetJobParameter<string>(CorrelationIdKey);

        if (!string.IsNullOrEmpty(correlationId))
        {
            CurrentCorrelationId.Value = correlationId;

            _logger?.LogDebug(
                "Executing job [{JobId}] with CorrelationId: [{CorrelationId}]",
                context.BackgroundJob.Id,
                correlationId);
        }
    }

    public void OnPerformed(PerformedContext context)
    {
        var correlationId = context.GetJobParameter<string>(CorrelationIdKey);

        _logger?.LogDebug(
            "Job [{JobId}] completed. CorrelationId: [{CorrelationId}]",
            context.BackgroundJob.Id,
            correlationId);

        CurrentCorrelationId.Value = null;
    }
}
