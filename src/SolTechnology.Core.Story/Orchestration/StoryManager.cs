using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Orchestration;

/// <summary>
/// Manages story lifecycle for interactive workflows with persistence.
/// Creates a fresh DI scope for every <c>StartStory</c>/<c>ResumeStory</c>/<c>CancelStory</c> call
/// to avoid captive-dependency issues (review §3.2).
/// </summary>
public class StoryManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStoryRepository _repository;
    private readonly ILogger<StoryManager> _logger;

    public StoryManager(
        IServiceScopeFactory scopeFactory,
        IStoryRepository repository,
        ILogger<StoryManager> logger)
    {
        _scopeFactory = scopeFactory;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Starts a new story execution.</summary>
    /// <param name="input">Initial input for the story.</param>
    /// <param name="idempotencyKey">
    /// Optional caller-supplied idempotency key. If a story with the same key already exists,
    /// its current state is returned instead of starting a new story.
    /// </param>
    public async Task<Result<StoryInstance>> StartStory<THandler, TInput, TContext, TOutput>(
        TInput input,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        where THandler : StoryHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await _repository.FindByIdempotencyKey(idempotencyKey!);
            if (existing != null) return Result<StoryInstance>.Success(existing);
        }

        return await ExecuteStoryPipeline<THandler, TInput, TContext, TOutput>(
            input, null, Auid.Empty, idempotencyKey, cancellationToken);
    }

    /// <summary>Resumes a paused story with user input.</summary>
    public async Task<Result<StoryInstance>> ResumeStory<THandler, TInput, TContext, TOutput>(
        Auid storyId,
        JsonElement? userInput = null,
        CancellationToken cancellationToken = default)
        where THandler : StoryHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        return await ExecuteStoryPipeline<THandler, TInput, TContext, TOutput>(
            null, userInput, storyId, null, cancellationToken);
    }

    /// <summary>
    /// Cancels a running or paused story. Sets its terminal status to <see cref="StoryStatus.Cancelled"/>.
    /// </summary>
    public async Task<Result<StoryInstance>> CancelStory(Auid storyId)
    {
        var instance = await _repository.FindById(storyId);
        if (instance == null) return Result<StoryInstance>.Fail($"Story {storyId} not found");
        if (instance.Status == StoryStatus.Completed)
            return Result<StoryInstance>.Fail("Cannot cancel a completed story");
        if (instance.Status == StoryStatus.Cancelled)
            return Result<StoryInstance>.Success(instance);

        instance.Status = StoryStatus.Cancelled;
        instance.LastUpdatedAt = DateTime.UtcNow;
        instance.CurrentChapter = null;
        await _repository.SaveAsync(instance);
        return Result<StoryInstance>.Success(instance);
    }

    private async Task<Result<StoryInstance>> ExecuteStoryPipeline<THandler, TInput, TContext, TOutput>(
        TInput? input,
        JsonElement? userInput,
        Auid storyId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
        where THandler : StoryHandler<TInput, TContext, TOutput>
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
                if (existing == null) return Result<StoryInstance>.Fail($"Story {storyId} not found");
                if (existing.Status == StoryStatus.Completed)
                    return Result<StoryInstance>.Fail("Story already completed");
                if (existing.Status == StoryStatus.Cancelled)
                    return Result<StoryInstance>.Fail("Story has been cancelled");

                try
                {
                    context = JsonSerializer.Deserialize<TContext>(existing.Context, StoryJsonOptions.Default);
                }
                catch (JsonException ex)
                {
                    return Result<StoryInstance>.Fail(
                        $"Persisted context for story {storyId} is corrupt and cannot be deserialized: {ex.Message}");
                }

                if (context == null)
                    return Result<StoryInstance>.Fail($"Persisted context for story {storyId} deserialized to null");

                context.StoryInstanceId = storyId;
                input = context.Input;
            }

            if (input == null && context == null)
                return Result<StoryInstance>.Fail("Input is required to start a story");

            var handler = ActivatorUtilities.CreateInstance<THandler>(sp);
            if (context != null) handler.Context = context;

            var result = await handler.Handle(input!, userInput, cancellationToken);

            var activeId = handler.Context.StoryInstanceId;

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

            if (result.IsFailure && result.Error is StoryPausedError)
                return await GetLatestInstance(activeId);

            if (result.IsSuccess)
            {
                var instance = await _repository.FindById(activeId);
                if (instance != null) return Result<StoryInstance>.Success(instance);

                // Story ran to completion without persistence being triggered by the handler.
                return Result<StoryInstance>.Success(new StoryInstance
                {
                    StoryId = activeId,
                    HandlerTypeName = typeof(THandler).Name,
                    Status = StoryStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                });
            }

            return Result<StoryInstance>.Fail(result.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing story {Handler}", typeof(THandler).Name);
            return Result<StoryInstance>.Fail(ex.Message);
        }
    }

    private async Task<Result<StoryInstance>> GetLatestInstance(Auid storyId)
    {
        var instance = await _repository.FindById(storyId);
        return instance != null
            ? Result<StoryInstance>.Success(instance)
            : Result<StoryInstance>.Fail("Instance not found after execution");
    }

    /// <summary>Retrieves the current state of a story.</summary>
    public async Task<Result<StoryInstance>> GetStoryState(Auid storyId)
    {
        try
        {
            var instance = await _repository.FindById(storyId);
            return instance != null
                ? Result<StoryInstance>.Success(instance)
                : Result<StoryInstance>.Fail("Story not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load story state for {StoryId}", storyId);
            return Result<StoryInstance>.Fail($"Failed to load story: {ex.Message}");
        }
    }
}
