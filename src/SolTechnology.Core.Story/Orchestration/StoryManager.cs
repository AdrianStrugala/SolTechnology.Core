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
    public async Task<Result<StoryInstance>> StartStory<THandler, TInput, TNarration, TOutput>(
        TInput input)
        where THandler : StoryHandler<TInput, TNarration, TOutput>
        where TInput : class
        where TNarration : Narration<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        _logger.LogInformation("Starting new story {HandlerType}", typeof(THandler).Name);

        try
        {
            // Create handler with persistence options
            var logger = _serviceProvider.GetRequiredService<ILogger<THandler>>();
            var options = new StoryOptions
            {
                EnablePersistence = true,
                Repository = _repository
            };

            var handler = (THandler)Activator.CreateInstance(
                typeof(THandler),
                _serviceProvider,
                logger,
                options)!;

            // Execute the story
            var result = await handler.Handle(input);

            // If story is paused, return the instance
            if (result.IsFailure && result.Error?.Message.Contains("paused") == true)
            {
                var storyId = handler.Narration.StoryInstanceId;
                if (!string.IsNullOrEmpty(storyId))
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
                var completedInstance = new StoryInstance
                {
                    StoryId = handler.Narration.StoryInstanceId ?? Guid.NewGuid().ToString(),
                    HandlerTypeName = typeof(THandler).Name,
                    Status = StoryStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow,
                    History = new List<ChapterInfo>()
                };

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
    public async Task<Result<StoryInstance>> ResumeStory<THandler, TInput, TNarration, TOutput>(
        string storyId,
        JsonElement? userInput = null)
        where THandler : StoryHandler<TInput, TNarration, TOutput>
        where TInput : class
        where TNarration : Narration<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        _logger.LogInformation("Resuming story {StoryId}", storyId);

        try
        {
            // Load the story instance
            var storyInstance = await _repository.FindById(storyId);
            if (storyInstance == null)
            {
                return Result<StoryInstance>.Fail($"Story {storyId} not found");
            }

            // Deserialize the narration context with consistent JSON options
            var narration = JsonSerializer.Deserialize<TNarration>(storyInstance.Context, StoryJsonOptions.Default);
            if (narration == null)
            {
                return Result<StoryInstance>.Fail("Failed to deserialize story context");
            }

            // Restore the story ID
            narration.StoryInstanceId = storyId;

            // Create handler with options for persistence
            var logger = _serviceProvider.GetRequiredService<ILogger<THandler>>();
            var options = new StoryOptions
            {
                EnablePersistence = true,
                Repository = _repository
            };

            // Create handler instance using reflection (since we need to pass options)
            var handler = (THandler)Activator.CreateInstance(
                typeof(THandler),
                _serviceProvider,
                logger,
                options)!;

            // Set the narration with restored context
            handler.Narration = narration;

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
            var result = await handler.Handle(narration.Input);

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
    public async Task<Result<StoryInstance>> GetStoryState(string storyId)
    {
        var storyInstance = await _repository.FindById(storyId);
        if (storyInstance == null)
        {
            return Result<StoryInstance>.Fail($"Story {storyId} not found");
        }

        return Result<StoryInstance>.Success(storyInstance);
    }
}
