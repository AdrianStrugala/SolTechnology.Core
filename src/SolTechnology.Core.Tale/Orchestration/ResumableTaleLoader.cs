using System.Text.Json;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Loads a stored tale for <c>ResumeStory</c>: rejects tales that cannot be resumed and restores the
/// typed context from its JSON. A Running tale is resumable only once its stored
/// <see cref="TaleInstance.LeaseExpiresAt"/> has passed. A Running tale without a stored expiry (a
/// repository that drops the field, or a row written before it existed) is held until its
/// <see cref="TaleInstance.LastUpdatedAt"/> plus this host's <see cref="TaleOptions.RunningLeaseTimeout"/>,
/// so a broken repository fails closed instead of handing a live run to a second host.
/// </summary>
internal sealed class ResumableTaleLoader(
    ITaleRepository repository,
    TimeProvider timeProvider,
    TimeSpan runningLeaseTimeout,
    ILogger logger)
{
    public async Task<Result<(TContext Context, TaleInstance? Instance)>> Load<TInput, TContext, TOutput>(
        Auid taleId, CancellationToken cancellationToken)
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        var existing = await repository.FindById(taleId, cancellationToken);
        var rejection = Reject(taleId, existing);
        if (rejection != null)
        {
            return Result<(TContext, TaleInstance?)>.Fail(rejection);
        }

        if (existing!.Status == TaleStatus.Running)
        {
            logger.LogWarning("Tale [{TaleId}] Running lease expired at [{LeaseExpiresAt}] (now [{Now}]); taking it over",
                taleId, existing.LeaseExpiresAt, timeProvider.GetUtcNow());
        }

        TContext? context;
        try
        {
            context = JsonSerializer.Deserialize<TContext>(existing.Context, TaleJsonOptions.Default);
        }
        catch (JsonException ex)
        {
            return Result<(TContext, TaleInstance?)>.Fail(
                $"Persisted context for tale {taleId} is corrupt and cannot be deserialized: {ex.Message}");
        }

        if (context == null)
        {
            return Result<(TContext, TaleInstance?)>.Fail($"Persisted context for tale {taleId} deserialized to null");
        }

        context.TaleInstanceId = taleId;
        return Result<(TContext, TaleInstance?)>.Success((context, existing));
    }

    private Error? Reject(Auid taleId, TaleInstance? existing) => existing switch
    {
        null => new Error { Message = $"Tale {taleId} not found" },
        { Status: TaleStatus.Completed } => new Error { Message = "Tale already completed" },
        { Status: TaleStatus.Cancelled } => new Error { Message = "Tale has been cancelled" },
        { Status: TaleStatus.Failed } => new ConflictError { Message = "Tale has failed and cannot be resumed" },
        { Status: TaleStatus.Running } running when IsLeaseHeld(running) => new ConflictError { Message = "Tale is already running" },
        _ when existing.History.Any(h => h.StepIndex is null) => new Error
        {
            Message = $"Tale {taleId} was created by SolTechnology.Core.Tale 1.x and cannot be resumed by 2.x; restart the tale."
        },
        _ => null
    };

    // The writer stamped the expiry with its own timeout, so hosts with different timeouts agree.
    private bool IsLeaseHeld(TaleInstance running)
    {
        var now = timeProvider.GetUtcNow();
        if (running.LeaseExpiresAt is { } expiresAt)
        {
            return expiresAt > now;
        }

        var fallbackExpiresAt = AsUtc(running.LastUpdatedAt) + runningLeaseTimeout;
        if (fallbackExpiresAt <= now)
        {
            return false;
        }

        logger.LogWarning("Tale [{TaleId}] is Running without a stored LeaseExpiresAt; refusing takeover until [{FallbackLeaseExpiresAt}] (LastUpdatedAt plus RunningLeaseTimeout [{RunningLeaseTimeout}]). ITaleRepository.Save must store LeaseExpiresAt",
            running.TaleId, fallbackExpiresAt, runningLeaseTimeout);
        return true;
    }

    // A DateTime of unspecified kind is read as UTC, never in the host's local zone.
    private static DateTimeOffset AsUtc(DateTime value) => value.Kind == DateTimeKind.Local
        ? new DateTimeOffset(value)
        : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
