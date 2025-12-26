using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Orchestration;

/// <summary>
/// High-level orchestration manager for stories.
/// Provides start/resume capabilities for complex pausable workflows.
/// </summary>
public class StoryManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IStoryRepository _repository;
    private readonly ILogger<StoryManager> _logger;

    public StoryManager(
        IServiceProvider serviceProvider,
        IStoryRepository repository,
        ILogger<StoryManager> logger)
    {
        _serviceProvider = serviceProvider;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Start a new story with the given input.
    /// Returns the story instance with its ID for future resume operations.
    /// </summary>
    public async Task<Result<StoryInstance>> StartStory<THandler, TInput, TContext, TOutput>(
        TInput input)
        where THandler : StoryHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        _logger.LogInformation("Starting new story {HandlerType}", typeof(THandler).Name);

        try
        {
            // Create handler - repository is already registered in DI
            var logger = _serviceProvider.GetRequiredService<ILogger<THandler>>();

            var handler = (THandler)Activator.CreateInstance(
                typeof(THandler),
                _serviceProvider,
                logger)!;

            // Execute the story
            var result = await handler.Handle(input);

            // If story is paused, return the instance
            if (result.IsFailure && result.Error?.Message.Contains("paused") == true)
            {
                var storyId = handler.Context.StoryInstanceId;
                if (storyId != Auid.Empty)
                {
                    var storyInstance = await _repository.FindById(storyId);
                    if (storyInstance != null)
                    {
                        return Result<StoryInstance>.Success(storyInstance);
                    }
                }
            }

            // Story completed or failed
            if (result.IsSuccess)
            {
                _logger.LogInformation("Story {HandlerType} completed successfully", typeof(THandler).Name);

                // Create a completed story instance
                var storyId = handler.Context.StoryInstanceId != Auid.Empty
                    ? handler.Context.StoryInstanceId
                    : Auid.New("STR");

                var completedInstance = new StoryInstance
                {
                    StoryId = storyId,
                    HandlerTypeName = typeof(THandler).Name,
                    Status = StoryStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow,
                    History = new List<ChapterInfo>()
                };

                // Save the completed story to repository
                try
                {
                    await _repository.SaveAsync(completedInstance);
                }
                catch (Exception saveEx)
                {
                    _logger.LogWarning(saveEx, "Failed to save completed story {StoryId}, but story execution succeeded", storyId);
                    // Don't fail the overall operation - story execution was successful
                }

                return Result<StoryInstance>.Success(completedInstance);
            }

            return Result<StoryInstance>.Fail(result.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start story {HandlerType}", typeof(THandler).Name);
            return Result<StoryInstance>.Fail($"Failed to start story: {ex.Message}");
        }
    }

    /// <summary>
    /// Resume a paused story with user input for the interactive chapter.
    /// </summary>
    public async Task<Result<StoryInstance>> ResumeStory<THandler, TInput, TContext, TOutput>(
        Auid storyId,
        JsonElement? userInput = null)
        where THandler : StoryHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        _logger.LogInformation("Resuming story {StoryId}", storyId);

        try
        {
            // Load the story instance
            StoryInstance? storyInstance;
            try
            {
                storyInstance = await _repository.FindById(storyId);
            }
            catch (Exception repoEx)
            {
                _logger.LogError(repoEx, "Repository error while loading story {StoryId}", storyId);
                return Result<StoryInstance>.Fail($"Failed to load story from storage: {repoEx.Message}");
            }

            if (storyInstance == null)
            {
                return Result<StoryInstance>.Fail($"Story {storyId} not found");
            }

            // Check if story is already completed
            if (storyInstance.Status == StoryStatus.Completed)
            {
                return Result<StoryInstance>.Fail($"Story {storyId} is already completed and cannot be resumed");
            }

            // Deserialize the context with consistent JSON options
            var context = JsonSerializer.Deserialize<TContext>(storyInstance.Context, StoryJsonOptions.Default);
            if (context == null)
            {
                return Result<StoryInstance>.Fail("Failed to deserialize story context");
            }

            // Restore the story ID
            context.StoryInstanceId = storyId;

            // Create handler - repository is already registered in DI
            var logger = _serviceProvider.GetRequiredService<ILogger<THandler>>();

            // Create handler instance using reflection
            var handler = (THandler)Activator.CreateInstance(
                typeof(THandler),
                _serviceProvider,
                logger)!;

            // Set the Context with restored context
            handler.Context = context;

            // If user input is provided, we need to pass it to the engine
            // This is done through the handler's internal engine via reflection
            if (userInput != null)
            {
                var engineField = typeof(THandler).BaseType!
                    .GetField("_engine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (engineField != null)
                {
                    var engine = engineField.GetValue(handler);
                    var setInputMethod = engine?.GetType()
                        .GetMethod("SetChapterInput", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    setInputMethod?.Invoke(engine, new object?[] { userInput });
                }
            }

            // Execute the story
            var result = await handler.Handle(context.Input);

            // Return updated story instance
            if (result.IsFailure && result.Error?.Message.Contains("paused") == true)
            {
                var updatedInstance = await _repository.FindById(storyId);
                if (updatedInstance != null)
                {
                    return Result<StoryInstance>.Success(updatedInstance);
                }
            }

            // Story completed - get the latest version from repository
            if (result.IsSuccess)
            {
                var updatedInstance = await _repository.FindById(storyId);
                if (updatedInstance != null)
                {
                    updatedInstance.Status = StoryStatus.Completed;
                    updatedInstance.LastUpdatedAt = DateTime.UtcNow;
                    await _repository.SaveAsync(updatedInstance);

                    return Result<StoryInstance>.Success(updatedInstance);
                }
            }

            return Result<StoryInstance>.Fail(result.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume story {StoryId}", storyId);
            return Result<StoryInstance>.Fail($"Failed to resume story: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the current state of a story.
    /// </summary>
    public async Task<Result<StoryInstance>> GetStoryState(Auid storyId)
    {
        try
        {
            var storyInstance = await _repository.FindById(storyId);
            if (storyInstance == null)
            {
                return Result<StoryInstance>.Fail($"Story {storyId} not found");
            }

            return Result<StoryInstance>.Success(storyInstance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve story state for {StoryId}", storyId);
            return Result<StoryInstance>.Fail($"Failed to retrieve story from storage: {ex.Message}");
        }
    }
}
