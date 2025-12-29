using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Orchestration;

/// <summary>
/// Manages story lifecycle for interactive workflows with persistence.
/// Handles starting, resuming, and querying story state.
/// </summary>
public class StoryManager(
    IServiceProvider serviceProvider,
    IStoryRepository repository,
    ILogger<StoryManager> logger)
{
    /// <summary>
    /// Starts a new story execution.
    /// </summary>
    /// <typeparam name="THandler">The story handler type</typeparam>
    /// <typeparam name="TInput">The input type</typeparam>
    /// <typeparam name="TContext">The context type</typeparam>
    /// <typeparam name="TOutput">The output type</typeparam>
    /// <param name="input">Initial input for the story</param>
    /// <returns>Story instance with state and ID</returns>
    public async Task<Result<StoryInstance>> StartStory<THandler, TInput, TContext, TOutput>(
        TInput input)
        where THandler : StoryHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        return await ExecuteStoryPipeline<THandler, TInput, TContext, TOutput>(input, null, Auid.Empty);
    }

    /// <summary>
    /// Resumes a paused story with user input.
    /// </summary>
    /// <typeparam name="THandler">The story handler type</typeparam>
    /// <typeparam name="TInput">The input type</typeparam>
    /// <typeparam name="TContext">The context type</typeparam>
    /// <typeparam name="TOutput">The output type</typeparam>
    /// <param name="storyId">ID of the story to resume</param>
    /// <param name="userInput">User input for the interactive chapter</param>
    /// <returns>Updated story instance</returns>
    public async Task<Result<StoryInstance>> ResumeStory<THandler, TInput, TContext, TOutput>(
        Auid storyId,
        JsonElement? userInput = null)
        where THandler : StoryHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        return await ExecuteStoryPipeline<THandler, TInput, TContext, TOutput>(null!, userInput, storyId);
    }

    private async Task<Result<StoryInstance>> ExecuteStoryPipeline<THandler, TInput, TContext, TOutput>(
        TInput? input,
        JsonElement? userInput,
        Auid storyId)
        where THandler : StoryHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        try
        {
            TContext? context = null;

            // 1. Resume Logic: Load and Deserialize Context
            if (storyId != Auid.Empty)
            {
                var existingInstance = await repository.FindById(storyId);
                if (existingInstance == null) return Result<StoryInstance>.Fail($"Story {storyId} not found");
                if (existingInstance.Status == StoryStatus.Completed) return Result<StoryInstance>.Fail("Story already completed");

                context = JsonSerializer.Deserialize<TContext>(existingInstance.Context, StoryJsonOptions.Default);
                if (context != null) context.StoryInstanceId = storyId;
                
                // If resuming, input comes from context, not parameter
                if (context != null) input = context.Input;
            }

            if (input == null && context == null)
                return Result<StoryInstance>.Fail("Input is required to start a story");

            // 2. Create Handler using ActivatorUtilities (Allows extra DI in Handler constructor)
            var handler = ActivatorUtilities.CreateInstance<THandler>(serviceProvider);
            
            if (context != null) handler.Context = context;

            // 3. Execute Handler (Pass userInput officially)
            Result<TOutput> result = await handler.Handle(input!, userInput);

            // 4. Handle Result State
            Auid activeStoryId = handler.Context.StoryInstanceId;

            // Case A: Paused (Waiting for input)
            if (result.IsFailure && result.Error?.Message.Contains("paused") == true)
            {
                return await GetLatestInstance(activeStoryId);
            }

            // Case B: Completed Successfully
            if (result.IsSuccess)
            {
                var completedInstance = await MarkAsCompleted(activeStoryId, typeof(THandler).Name);
                return Result<StoryInstance>.Success(completedInstance);
            }

            // Case C: Failed
            return Result<StoryInstance>.Fail(result.Error!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing story {Handler}", typeof(THandler).Name);
            return Result<StoryInstance>.Fail(ex.Message);
        }
    }

    private async Task<Result<StoryInstance>> GetLatestInstance(Auid storyId)
    {
        var instance = await repository.FindById(storyId);
        return instance != null 
            ? Result<StoryInstance>.Success(instance) 
            : Result<StoryInstance>.Fail("Instance not found after execution");
    }

    private async Task<StoryInstance> MarkAsCompleted(Auid storyId, string handlerName)
    {
        // Check if exists (Resume scenario)
        var instance = await repository.FindById(storyId);
        
        if (instance == null)
        {
            // Create new for one-shot stories
            instance = new StoryInstance
            {
                StoryId = storyId,
                HandlerTypeName = handlerName,
                CreatedAt = DateTime.UtcNow,
                History = new List<ChapterInfo>()
            };
        }

        instance.Status = StoryStatus.Completed;
        instance.LastUpdatedAt = DateTime.UtcNow;

        try
        {
            await repository.SaveAsync(instance);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save completion status for {StoryId}", storyId);
        }

        return instance;
    }

    /// <summary>
    /// Retrieves the current state of a story.
    /// </summary>
    /// <param name="storyId">The story ID</param>
    /// <returns>Story instance with current state, history, and context</returns>
    public async Task<Result<StoryInstance>> GetStoryState(Auid storyId)
    {
        try
        {
            var instance = await repository.FindById(storyId);
            return instance != null
                ? Result<StoryInstance>.Success(instance)
                : Result<StoryInstance>.Fail("Story not found");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load story state for {StoryId}", storyId);
            return Result<StoryInstance>.Fail($"Failed to load story: {ex.Message}");
        }
    }
}