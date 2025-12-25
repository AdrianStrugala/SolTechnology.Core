using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Orchestration;

namespace SolTechnology.Core.Story.Api;

/// <summary>
/// Base REST API controller for managing Story Framework workflows.
/// Provides endpoints to start, resume, and query story instances.
/// Can be inherited in your application to customize behavior or add authentication.
/// </summary>
[ApiController]
[Route("api/story")]
public class StoryController : ControllerBase
{
    protected readonly StoryManager StoryManager;
    protected readonly ILogger<StoryController> Logger;

    public StoryController(
        StoryManager storyManager,
        ILogger<StoryController> logger)
    {
        StoryManager = storyManager;
        Logger = logger;
    }

    /// <summary>
    /// Start a new story with the given input.
    /// Returns the story instance ID and current status.
    /// </summary>
    /// <param name="handlerTypeName">The name of the StoryHandler type (e.g., "SampleOrderWorkflowHandler")</param>
    /// <param name="input">The input data as JSON</param>
    /// <returns>Story instance with ID and current status</returns>
    [HttpPost("{handlerTypeName}/start")]
    public virtual async Task<IActionResult> StartStory(
        [FromRoute] string handlerTypeName,
        [FromBody] JsonElement input)
    {
        Logger.LogInformation("Starting story: {HandlerType}", handlerTypeName);

        try
        {
            // Resolve handler type from all loaded assemblies
            var handlerType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == handlerTypeName && !t.IsAbstract);

            if (handlerType == null)
            {
                return NotFound(Result.Fail($"Story handler '{handlerTypeName}' not found"));
            }

            // Get generic type arguments from StoryHandler base
            var baseType = handlerType.BaseType;
            while (baseType != null && (!baseType.IsGenericType || baseType.GetGenericTypeDefinition().Name != "StoryHandler`3"))
            {
                baseType = baseType.BaseType;
            }

            if (baseType == null)
            {
                return BadRequest(Result.Fail($"Type '{handlerTypeName}' is not a valid StoryHandler"));
            }

            var genericArgs = baseType.GetGenericArguments();
            var inputType = genericArgs[0];
            var narrationType = genericArgs[1];
            var outputType = genericArgs[2];

            // Deserialize input to correct type
            var typedInput = JsonSerializer.Deserialize(input.GetRawText(), inputType);
            if (typedInput == null)
            {
                return BadRequest(Result.Fail("Failed to deserialize input"));
            }

            // Call StartStory via reflection
            var startStoryMethod = typeof(StoryManager)
                .GetMethod(nameof(StoryManager.StartStory))!
                .MakeGenericMethod(handlerType, inputType, narrationType, outputType);

            var task = (Task)startStoryMethod.Invoke(StoryManager, new[] { typedInput })!;
            await task;

            var resultProperty = task.GetType().GetProperty("Result")!;
            var result = resultProperty.GetValue(task);

            // Extract Result<StoryInstance>
            var isSuccessProperty = result!.GetType().GetProperty("IsSuccess")!;
            var isSuccess = (bool)isSuccessProperty.GetValue(result)!;

            if (!isSuccess)
            {
                var errorProperty = result.GetType().GetProperty("Error")!;
                var error = errorProperty.GetValue(result);

                // Check if it's a pause (which is not really an error)
                var errorMessage = error?.GetType().GetProperty("Message")?.GetValue(error)?.ToString();
                if (errorMessage?.Contains("paused") == true)
                {
                    // Story paused - return the instance
                    var dataProperty = result.GetType().GetProperty("Data")!;
                    var storyInstance = (StoryInstance)dataProperty.GetValue(result)!;

                    return Ok(Result<StoryInstanceDto>.Success(StoryInstanceDto.FromModel(storyInstance)));
                }

                return BadRequest(result);
            }

            // Success case
            var dataProperty2 = result.GetType().GetProperty("Data")!;
            var storyInstance2 = (StoryInstance)dataProperty2.GetValue(result)!;

            return Ok(Result<StoryInstanceDto>.Success(StoryInstanceDto.FromModel(storyInstance2)));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start story {HandlerType}", handlerTypeName);
            return StatusCode(500, Result.Fail($"Failed to start story: {ex.Message}"));
        }
    }

    /// <summary>
    /// Resume a paused story with user input.
    /// If no input is provided, attempts to continue without input.
    /// </summary>
    /// <param name="storyId">The story instance ID</param>
    /// <param name="userInput">Optional user input for interactive chapters</param>
    /// <returns>Updated story instance</returns>
    [HttpPost("{storyId}")]
    public virtual async Task<IActionResult> ResumeStory(
        [FromRoute] string storyId,
        [FromBody] JsonElement? userInput = null)
    {
        Logger.LogInformation("Resuming story: {StoryId}", storyId);

        try
        {
            // Parse storyId to Auid
            if (!Auid.TryParse(storyId, null, out var parsedStoryId))
            {
                return BadRequest(Result.Fail($"Invalid story ID format: {storyId}"));
            }

            // Get current story state
            var stateResult = await StoryManager.GetStoryState(parsedStoryId);
            if (stateResult.IsFailure)
            {
                return NotFound(stateResult);
            }

            var storyInstance = stateResult.Data!;

            // Resolve handler type
            var handlerType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == storyInstance.HandlerTypeName && !t.IsAbstract);

            if (handlerType == null)
            {
                return NotFound(Result.Fail($"Story handler '{storyInstance.HandlerTypeName}' not found"));
            }

            // Get generic type arguments
            var baseType = handlerType.BaseType;
            while (baseType != null && (!baseType.IsGenericType || baseType.GetGenericTypeDefinition().Name != "StoryHandler`3"))
            {
                baseType = baseType.BaseType;
            }

            if (baseType == null)
            {
                return BadRequest(Result.Fail($"Type '{storyInstance.HandlerTypeName}' is not a valid StoryHandler"));
            }

            var genericArgs = baseType.GetGenericArguments();
            var inputType = genericArgs[0];
            var narrationType = genericArgs[1];
            var outputType = genericArgs[2];

            // Call ResumeStory via reflection
            var resumeStoryMethod = typeof(StoryManager)
                .GetMethod(nameof(StoryManager.ResumeStory))!
                .MakeGenericMethod(handlerType, inputType, narrationType, outputType);

            var task = (Task)resumeStoryMethod.Invoke(StoryManager, new object?[] { parsedStoryId, userInput })!;
            await task;

            var resultProperty = task.GetType().GetProperty("Result")!;
            var result = resultProperty.GetValue(task);

            // Extract Result<StoryInstance>
            var isSuccessProperty = result!.GetType().GetProperty("IsSuccess")!;
            var isSuccess = (bool)isSuccessProperty.GetValue(result)!;

            if (!isSuccess)
            {
                var errorProperty = result.GetType().GetProperty("Error")!;
                var error = errorProperty.GetValue(result);

                // Check if it's a pause
                var errorMessage = error?.GetType().GetProperty("Message")?.GetValue(error)?.ToString();
                if (errorMessage?.Contains("paused") == true)
                {
                    var dataProperty = result.GetType().GetProperty("Data")!;
                    var updatedInstance = (StoryInstance)dataProperty.GetValue(result)!;

                    return Ok(Result<StoryInstanceDto>.Success(StoryInstanceDto.FromModel(updatedInstance)));
                }

                return BadRequest(result);
            }

            var dataProperty2 = result.GetType().GetProperty("Data")!;
            var updatedInstance2 = (StoryInstance)dataProperty2.GetValue(result)!;

            return Ok(Result<StoryInstanceDto>.Success(StoryInstanceDto.FromModel(updatedInstance2)));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to resume story {StoryId}", storyId);
            return StatusCode(500, Result.Fail($"Failed to resume story: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get the current state of a story.
    /// </summary>
    /// <param name="storyId">The story instance ID</param>
    /// <returns>Story instance with current status and history</returns>
    [HttpGet("{storyId}")]
    public virtual async Task<IActionResult> GetStoryState([FromRoute] string storyId)
    {
        Logger.LogInformation("Getting story state: {StoryId}", storyId);

        // Parse storyId to Auid
        if (!Auid.TryParse(storyId, null, out var parsedStoryId))
        {
            return BadRequest(Result.Fail($"Invalid story ID format: {storyId}"));
        }

        var result = await StoryManager.GetStoryState(parsedStoryId);

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(Result<StoryInstanceDto>.Success(StoryInstanceDto.FromModel(result.Data!)));
    }

    /// <summary>
    /// Get the result of a completed story.
    /// </summary>
    /// <param name="storyId">The story instance ID</param>
    /// <returns>Story output if completed, otherwise current state</returns>
    [HttpGet("{storyId}/result")]
    public virtual async Task<IActionResult> GetStoryResult([FromRoute] string storyId)
    {
        Logger.LogInformation("Getting story result: {StoryId}", storyId);

        // Parse storyId to Auid
        if (!Auid.TryParse(storyId, null, out var parsedStoryId))
        {
            return BadRequest(Result.Fail($"Invalid story ID format: {storyId}"));
        }

        var stateResult = await StoryManager.GetStoryState(parsedStoryId);

        if (stateResult.IsFailure)
        {
            return NotFound(stateResult);
        }

        var storyInstance = stateResult.Data!;

        if (storyInstance.Status != StoryStatus.Completed)
        {
            return BadRequest(Result.Fail($"Story is not completed. Current status: {storyInstance.Status}"));
        }

        // Deserialize context to get output
        // For now, return the full story instance
        // In a real implementation, you might want to extract just the Output from the narration
        return Ok(Result<StoryInstanceDto>.Success(StoryInstanceDto.FromModel(storyInstance)));
    }
}

/// <summary>
/// DTO for StoryInstance to control JSON serialization
/// </summary>
public class StoryInstanceDto
{
    public string StoryId { get; set; } = default!;
    public string HandlerTypeName { get; set; } = default!;
    public StoryStatus Status { get; set; }
    public ChapterInfo? CurrentChapter { get; set; }
    public List<ChapterInfo> History { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    public static StoryInstanceDto FromModel(StoryInstance instance)
    {
        return new StoryInstanceDto
        {
            StoryId = instance.StoryId.ToString(),
            HandlerTypeName = instance.HandlerTypeName,
            Status = instance.Status,
            CurrentChapter = instance.CurrentChapter,
            History = instance.History,
            CreatedAt = instance.CreatedAt,
            LastUpdatedAt = instance.LastUpdatedAt
        };
    }
}
