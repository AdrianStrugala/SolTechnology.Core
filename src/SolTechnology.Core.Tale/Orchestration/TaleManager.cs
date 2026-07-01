using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Manages story lifecycle for interactive workflows with persistence.
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

    /// <summary>Starts a new story execution.</summary>
    /// <param name="input">Initial input for the story.</param>
    /// <param name="idempotencyKey">
    /// Optional caller-supplied idempotency key. If a story with the same key already exists,
    /// its current state is returned instead of starting a new story.
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
            var existing = await _repository.FindByIdempotencyKey(idempotencyKey!);
            if (existing != null) return Result<TaleInstance>.Success(existing);
        }

        return await ExecuteTalePipeline<THandler, TInput, TContext, TOutput>(
            input, null, Auid.Empty, idempotencyKey, cancellationToken);
    }

    /// <summary>Resumes a paused story with user input.</summary>
    public async Task<Result<TaleInstance>> ResumeStory<THandler, TInput, TContext, TOutput>(
        Auid storyId,
        JsonElement? userInput = null,
        CancellationToken cancellationToken = default)
        where THandler : TaleHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        return await ExecuteTalePipeline<THandler, TInput, TContext, TOutput>(
            null, userInput, storyId, null, cancellationToken);
    }

    /// <summary>
    /// Cancels a running or paused story. Sets its terminal status to <see cref="TaleStatus.Cancelled"/>.
    /// </summary>
    public async Task<Result<TaleInstance>> CancelStory(Auid storyId)
    {
        var instance = await _repository.FindById(storyId);
        if (instance == null) return Result<TaleInstance>.Fail($"Story {storyId} not found");
        if (instance.Status == TaleStatus.Completed)
            return Result<TaleInstance>.Fail("Cannot cancel a completed story");
        if (instance.Status == TaleStatus.Cancelled)
            return Result<TaleInstance>.Success(instance);

        instance.Status = TaleStatus.Cancelled;
        instance.LastUpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        instance.CurrentChapter = null;
        await _repository.SaveAsync(instance);
        return Result<TaleInstance>.Success(instance);
    }

    private async Task<Result<TaleInstance>> ExecuteTalePipeline<THandler, TInput, TContext, TOutput>(
        TInput? input,
        JsonElement? userInput,
        Auid storyId,
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
            TContext? context = null;

            if (storyId != Auid.Empty)
            {
                var existing = await _repository.FindById(storyId);
                if (existing == null) return Result<TaleInstance>.Fail($"Story {storyId} not found");
                if (existing.Status == TaleStatus.Completed)
                    return Result<TaleInstance>.Fail("Tale already completed");
                if (existing.Status == TaleStatus.Cancelled)
                    return Result<TaleInstance>.Fail("Tale has been cancelled");

                try
                {
                    context = JsonSerializer.Deserialize<TContext>(existing.Context, TaleJsonOptions.Default);
                }
                catch (JsonException ex)
                {
                    return Result<TaleInstance>.Fail(
                        $"Persisted context for story {storyId} is corrupt and cannot be deserialized: {ex.Message}");
                }

                if (context == null)
                    return Result<TaleInstance>.Fail($"Persisted context for story {storyId} deserialized to null");

                context.TaleInstanceId = storyId;
                input = context.Input;
            }

            if (input == null && context == null)
                return Result<TaleInstance>.Fail("Input is required to start a story");

            var handler = ActivatorUtilities.CreateInstance<THandler>(sp);
            if (context != null) handler.Context = context;

            var result = await handler.Handle(input!, userInput, cancellationToken);

            var activeId = handler.Context.TaleInstanceId;

            // Stamp the idempotency key (if provided and this is a fresh story).
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var persisted = await _repository.FindById(activeId);
                if (persisted != null && persisted.IdempotencyKey == null)
                {
                    persisted.IdempotencyKey = idempotencyKey;
                    await _repository.SaveAsync(persisted);
                }
            }

            if (result.IsFailure && result.Error is TalePausedError)
                return await GetLatestInstance(activeId);

            if (result.IsSuccess)
            {
                var instance = await _repository.FindById(activeId);
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
            _logger.LogError(ex, "Error executing story {Handler}", typeof(THandler).Name);
            return Result<TaleInstance>.Fail(ex.Message);
        }
    }

    private async Task<Result<TaleInstance>> GetLatestInstance(Auid storyId)
    {
        var instance = await _repository.FindById(storyId);
        return instance != null
            ? Result<TaleInstance>.Success(instance)
            : Result<TaleInstance>.Fail("Instance not found after execution");
    }

    /// <summary>Retrieves the current state of a story.</summary>
    public async Task<Result<TaleInstance>> GetStoryState(Auid storyId)
    {
        try
        {
            var instance = await _repository.FindById(storyId);
            return instance != null
                ? Result<TaleInstance>.Success(instance)
                : Result<TaleInstance>.Fail("Tale not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tale state for {TaleId}", storyId);
            return Result<TaleInstance>.Fail($"Failed to load story: {ex.Message}");
        }
    }
}
