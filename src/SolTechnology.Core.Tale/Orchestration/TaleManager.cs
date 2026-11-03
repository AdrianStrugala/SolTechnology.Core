using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Manages tale lifecycle for interactive workflows with persistence.
/// Creates a fresh DI scope for every <c>StartStory</c>/<c>ResumeStory</c>/<c>CancelStory</c> call
/// to avoid captive-dependency issues (review §3.2).
/// </summary>
public class TaleManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITaleRepository _repository;
    private readonly ILogger<TaleManager> _logger;
    private readonly TimeProvider _timeProvider;

    public TaleManager(
        IServiceScopeFactory scopeFactory,
        ITaleRepository repository,
        ILogger<TaleManager> logger,
        TimeProvider? timeProvider = null)
    {
        _scopeFactory = scopeFactory;
        _repository = repository;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Starts a new tale execution.</summary>
    /// <param name="input">Initial input for the tale.</param>
    /// <param name="idempotencyKey">
    /// Optional caller-supplied idempotency key. If a tale with the same key already exists,
    /// its current state is returned instead of starting a new tale.
    /// </param>
    public async Task<Result<TaleInstance>> StartStory<THandler, TInput, TContext, TOutput>(
        TInput input,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        where THandler : TaleHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await _repository.FindByIdempotencyKey(idempotencyKey!, cancellationToken);
            if (existing != null) return Result<TaleInstance>.Success(existing);
        }

        return await ExecuteTalePipeline<THandler, TInput, TContext, TOutput>(
            input, null, Auid.Empty, idempotencyKey, cancellationToken);
    }

    /// <summary>Resumes a paused tale with user input.</summary>
    public async Task<Result<TaleInstance>> ResumeStory<THandler, TInput, TContext, TOutput>(
        Auid taleId,
        JsonElement? userInput = null,
        CancellationToken cancellationToken = default)
        where THandler : TaleHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        return await ExecuteTalePipeline<THandler, TInput, TContext, TOutput>(
            null, userInput, taleId, null, cancellationToken);
    }

    /// <summary>
    /// Cancels a running or paused tale. Sets its terminal status to <see cref="TaleStatus.Cancelled"/>.
    /// </summary>
    public async Task<Result<TaleInstance>> CancelStory(Auid taleId, CancellationToken cancellationToken = default)
    {
        var instance = await _repository.FindById(taleId, cancellationToken);
        if (instance == null) return Result<TaleInstance>.Fail($"Tale {taleId} not found");
        if (instance.Status == TaleStatus.Completed)
            return Result<TaleInstance>.Fail("Cannot cancel a completed tale");
        if (instance.Status == TaleStatus.Cancelled)
            return Result<TaleInstance>.Success(instance);

        instance.Status = TaleStatus.Cancelled;
        instance.LastUpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        instance.CurrentChapter = null;
        var saved = await _repository.Save(instance, cancellationToken);
        return saved.IsSuccess ? Result<TaleInstance>.Success(instance) : Result<TaleInstance>.Fail(saved.Error!);
    }

    private async Task<Result<TaleInstance>> ExecuteTalePipeline<THandler, TInput, TContext, TOutput>(
        TInput? input,
        JsonElement? userInput,
        Auid taleId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
        where THandler : TaleHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        // Dedicated DI scope for this invocation — avoids captive dependencies (Scoped DbContext etc.).
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        try
        {
            TContext? context;
            TaleInstance? existing = null;

            if (taleId != Auid.Empty)
            {
                existing = await _repository.FindById(taleId, cancellationToken);
                if (existing == null) return Result<TaleInstance>.Fail($"Tale {taleId} not found");
                if (existing.Status == TaleStatus.Completed)
                    return Result<TaleInstance>.Fail("Tale already completed");
                if (existing.Status == TaleStatus.Cancelled)
                    return Result<TaleInstance>.Fail("Tale has been cancelled");
                if (existing.Status == TaleStatus.Failed)
                    return Result<TaleInstance>.Fail(new ConflictError { Message = "Tale has failed and cannot be resumed" });

                try
                {
                    context = JsonSerializer.Deserialize<TContext>(existing.Context, TaleJsonOptions.Default);
                }
                catch (JsonException ex)
                {
                    return Result<TaleInstance>.Fail(
                        $"Persisted context for tale {taleId} is corrupt and cannot be deserialized: {ex.Message}");
                }

                if (context == null)
                    return Result<TaleInstance>.Fail($"Persisted context for tale {taleId} deserialized to null");

                context.TaleInstanceId = taleId;
            }
            else
            {
                if (input == null)
                    return Result<TaleInstance>.Fail("Input is required to start a tale");

                context = new TContext { Input = input };
            }

            var handler = ActivatorUtilities.CreateInstance<THandler>(sp);
            var run = new TaleRunOptions { Persist = true, Resume = existing };
            var result = await handler.Run(context, userInput, run, cancellationToken);

            var activeId = context.TaleInstanceId;

            // Stamp the idempotency key (if provided and this is a fresh tale).
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var persisted = await _repository.FindById(activeId, cancellationToken);
                if (persisted != null && persisted.IdempotencyKey == null)
                {
                    persisted.IdempotencyKey = idempotencyKey;
                    var stamped = await _repository.Save(persisted, cancellationToken);
                    if (stamped.IsFailure)
                        return Result<TaleInstance>.Fail(stamped.Error!);
                }
            }

            if (result.IsFailure && result.Error is TalePausedError)
                return await GetLatestInstance(activeId, cancellationToken);

            if (result.IsSuccess)
            {
                var instance = await _repository.FindById(activeId, cancellationToken);
                if (instance != null) return Result<TaleInstance>.Success(instance);

                // Tale ran to completion without persistence being triggered by the handler.
                return Result<TaleInstance>.Success(new TaleInstance
                {
                    TaleId = activeId,
                    HandlerTypeName = typeof(THandler).Name,
                    Status = TaleStatus.Completed,
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                    LastUpdatedAt = _timeProvider.GetUtcNow().UtcDateTime
                });
            }

            return Result<TaleInstance>.Fail(result.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tale {Handler}", typeof(THandler).Name);
            return Result<TaleInstance>.Fail($"Tale {typeof(THandler).Name} failed unexpectedly.");
        }
    }

    private async Task<Result<TaleInstance>> GetLatestInstance(Auid taleId, CancellationToken cancellationToken)
    {
        var instance = await _repository.FindById(taleId, cancellationToken);
        return instance != null
            ? Result<TaleInstance>.Success(instance)
            : Result<TaleInstance>.Fail("Instance not found after execution");
    }

    /// <summary>Retrieves the current state of a tale.</summary>
    public async Task<Result<TaleInstance>> GetStoryState(Auid taleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var instance = await _repository.FindById(taleId, cancellationToken);
            return instance != null
                ? Result<TaleInstance>.Success(instance)
                : Result<TaleInstance>.Fail("Tale not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tale state for {TaleId}", taleId);
            return Result<TaleInstance>.Fail("Failed to load tale.");
        }
    }
}
