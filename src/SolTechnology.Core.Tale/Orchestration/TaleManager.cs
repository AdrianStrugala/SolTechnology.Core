using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Manages tale lifecycle for interactive workflows with persistence.
/// Creates a fresh DI scope for every <c>StartStory</c>/<c>ResumeStory</c> call to avoid
/// captive-dependency issues.
/// </summary>
/// <param name="scopeFactory">Creates a DI scope per call.</param>
/// <param name="repository">Stores tale instances.</param>
/// <param name="logger">Logs unexpected failures.</param>
/// <param name="timeProvider">Clock for timestamps; <see cref="TimeProvider.System"/> when null.</param>
public partial class TaleManager(
    IServiceScopeFactory scopeFactory,
    ITaleRepository repository,
    ILogger<TaleManager> logger,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>Starts a new tale execution.</summary>
    /// <param name="input">Initial input for the tale.</param>
    /// <param name="idempotencyKey">
    /// Optional caller-supplied idempotency key. If a tale with the same key already exists,
    /// its current state is returned instead of starting a new tale.
    /// </param>
    /// <param name="cancellationToken">Cancels the run; passed to every chapter and repository call.</param>
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
            var existing = await repository.FindByIdempotencyKey(idempotencyKey, cancellationToken);
            if (existing != null)
            {
                return Result<TaleInstance>.Success(existing);
            }
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
        var instance = await repository.FindById(taleId, cancellationToken);
        if (instance == null)
        {
            return Result<TaleInstance>.Fail($"Tale {taleId} not found");
        }

        if (instance.Status == TaleStatus.Completed)
        {
            return Result<TaleInstance>.Fail("Cannot cancel a completed tale");
        }

        if (instance.Status == TaleStatus.Cancelled)
        {
            return Result<TaleInstance>.Success(instance);
        }

        instance.Status = TaleStatus.Cancelled;
        instance.LastUpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        instance.CurrentChapter = null;
        var saved = await repository.Save(instance, cancellationToken);
        return saved.IsSuccess ? Result<TaleInstance>.Success(instance) : Result<TaleInstance>.Fail(saved.Error!);
    }

    /// <summary>Retrieves the current state of a tale.</summary>
    public async Task<Result<TaleInstance>> GetStoryState(Auid taleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var instance = await repository.FindById(taleId, cancellationToken);
            return instance != null
                ? Result<TaleInstance>.Success(instance)
                : Result<TaleInstance>.Fail("Tale not found");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load tale state for {TaleId}", taleId);
            return Result<TaleInstance>.Fail("Failed to load tale.");
        }
    }
}
