using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;

namespace SolTechnology.Core.Tale.Api;

/// <summary>
/// Base REST API controller for the Tale Framework. Provides start/resume/cancel/query endpoints.
/// </summary>
/// <remarks>
/// Only handlers registered via <c>AddSolTale</c> are exposed — see <see cref="TaleHandlerRegistry"/>.
/// Inherit and annotate with your auth attributes before exposing publicly.
/// </remarks>
[ApiController]
[Route("api/tale")]
public abstract class TaleController(
    TaleManager storyManager,
    TaleHandlerRegistry registry,
    TaleOptions options,
    ILogger<TaleController> logger)
    : ControllerBase
{
    protected TaleManager TaleManager { get; } = storyManager;
    protected TaleHandlerRegistry Registry { get; } = registry;
    protected TaleOptions Options { get; } = options;
    protected ILogger<TaleController> Logger { get; } = logger;

    // Cached MethodInfo handles for TaleManager generic methods.
    private static readonly ConcurrentDictionary<(Type, string), MethodInfo> _methodCache = new();

    /// <summary>Start a new story with the given input.</summary>
    [HttpPost("{handlerTypeName}/start")]
    public virtual async Task<IActionResult> StartStory(
        [FromRoute] string handlerTypeName,
        [FromBody] JsonElement input,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting story: {HandlerType}", handlerTypeName);

        if (!TryResolveHandler(handlerTypeName, out var handlerType, out var error))
        {
            return error!;
        }

        if (!TryGetGenericArgs(handlerType!, out var args, out error))
        {
            return error!;
        }

        object? typedInput;
        try
        {
            typedInput = JsonSerializer.Deserialize(input.GetRawText(), args![0]);
        }
        catch (JsonException ex)
        {
            return BadRequest(Result.Fail($"Failed to deserialize input: {ex.Message}"));
        }

        if (typedInput == null)
        {
            return BadRequest(Result.Fail("Failed to deserialize input"));
        }

        try
        {
            var method = GetCachedMethod(nameof(TaleManager.StartStory))
                .MakeGenericMethod(handlerType!, args[0], args[1], args[2]);

            var task = (Task)method.Invoke(TaleManager, new object?[] { typedInput, idempotencyKey, cancellationToken })!;
            await task;
            return UnwrapResult(task);
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            Logger.LogError(tie.InnerException, "Failed to start story {HandlerType}", handlerTypeName);
            return StatusCode(500, Result.Fail(tie.InnerException.Message));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start story {HandlerType}", handlerTypeName);
            return StatusCode(500, Result.Fail(ex.Message));
        }
    }

    /// <summary>Resume a paused story with user input.</summary>
    [HttpPost("{storyId}")]
    public virtual async Task<IActionResult> ResumeStory(
        [FromRoute] string storyId,
        [FromBody] JsonElement? userInput = null,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Resuming story: {TaleId}", storyId);

        if (!Auid.TryParse(storyId, null, out var parsedStoryId))
        {
            return BadRequest(Result.Fail($"Invalid story ID format: {storyId}"));
        }

        var state = await TaleManager.GetStoryState(parsedStoryId);
        if (state.IsFailure)
        {
            return NotFound(state);
        }

        var handlerTypeName = state.Data!.HandlerTypeName;
        if (!TryResolveHandler(handlerTypeName, out var handlerType, out var error))
        {
            return error!;
        }

        if (!TryGetGenericArgs(handlerType!, out var args, out error))
        {
            return error!;
        }

        try
        {
            var method = GetCachedMethod(nameof(TaleManager.ResumeStory))
                .MakeGenericMethod(handlerType!, args![0], args[1], args[2]);

            var task = (Task)method.Invoke(TaleManager, new object?[] { parsedStoryId, userInput, cancellationToken })!;
            await task;
            return UnwrapResult(task);
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            Logger.LogError(tie.InnerException, "Failed to resume story {TaleId}", storyId);
            return StatusCode(500, Result.Fail(tie.InnerException.Message));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to resume story {TaleId}", storyId);
            return StatusCode(500, Result.Fail(ex.Message));
        }
    }

    /// <summary>Get the current state of a story.</summary>
    [HttpGet("{storyId}")]
    public virtual async Task<IActionResult> GetStoryState([FromRoute] string storyId)
    {
        if (!Auid.TryParse(storyId, null, out var parsedStoryId))
        {
            return BadRequest(Result.Fail($"Invalid story ID format: {storyId}"));
        }

        var result = await TaleManager.GetStoryState(parsedStoryId);
        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(Result<TaleInstanceDto>.Success(TaleInstanceDto.FromModel(result.Data!)));
    }

    /// <summary>Get the deserialized output of a completed story.</summary>
    [HttpGet("{storyId}/result")]
    public virtual async Task<IActionResult> GetStoryResult([FromRoute] string storyId)
    {
        if (!Auid.TryParse(storyId, null, out var parsedStoryId))
        {
            return BadRequest(Result.Fail($"Invalid story ID format: {storyId}"));
        }

        var state = await TaleManager.GetStoryState(parsedStoryId);
        if (state.IsFailure)
        {
            return NotFound(state);
        }

        var instance = state.Data!;
        if (instance.Status != TaleStatus.Completed)
        {
            return BadRequest(Result.Fail($"Tale is not completed. Current status: {instance.Status}"));
        }

        if (!TryResolveHandler(instance.HandlerTypeName, out var handlerType, out var error))
        {
            return error!;
        }

        if (!TryGetGenericArgs(handlerType!, out var args, out error))
        {
            return error!;
        }

        try
        {
            var context = JsonSerializer.Deserialize(instance.Context, args![1]);
            var outputProp = args[1].GetProperty("Output");
            var output = outputProp?.GetValue(context);

            return Ok(new TaleResultDto
            {
                TaleId = instance.TaleId.ToString(),
                Status = instance.Status,
                Output = output
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to extract result for story {TaleId}", storyId);
            return StatusCode(500, Result.Fail($"Failed to extract story output: {ex.Message}"));
        }
    }

    /// <summary>Cancel a running or paused story.</summary>
    [HttpDelete("{storyId}")]
    public virtual async Task<IActionResult> CancelStory([FromRoute] string storyId)
    {
        if (!Auid.TryParse(storyId, null, out var parsedStoryId))
        {
            return BadRequest(Result.Fail($"Invalid story ID format: {storyId}"));
        }

        var result = await TaleManager.CancelStory(parsedStoryId);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(Result<TaleInstanceDto>.Success(TaleInstanceDto.FromModel(result.Data!)));
    }

    // --- internals -----------------------------------------------------------

    private bool TryResolveHandler(string name, out Type? handlerType, out IActionResult? error)
    {
        error = null;
        if (Registry.TryResolve(name, out var resolved))
        {
            handlerType = resolved;
            return true;
        }

        handlerType = null;
        error = NotFound(Result.Fail($"Tale handler '{name}' is not registered."));
        return false;
    }

    private static bool TryGetGenericArgs(Type handlerType, out Type[]? args, out IActionResult? error)
    {
        var t = handlerType.BaseType;
        while (t != null && t != typeof(object))
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition().Name == "TaleHandler`3")
            {
                args = t.GetGenericArguments();
                error = null;
                return true;
            }
            t = t.BaseType;
        }

        args = null;
        error = new BadRequestObjectResult(Result.Fail($"Type '{handlerType.Name}' is not a TaleHandler"));
        return false;
    }

    private static MethodInfo GetCachedMethod(string methodName) =>
        _methodCache.GetOrAdd((typeof(TaleManager), methodName), key =>
            typeof(TaleManager).GetMethod(key.Item2)
            ?? throw new InvalidOperationException($"TaleManager method '{key.Item2}' not found."));

    private IActionResult UnwrapResult(Task task)
    {
        var resultProp = task.GetType().GetProperty("Result")!;
        var raw = resultProp.GetValue(task);
        var isSuccess = (bool)raw!.GetType().GetProperty("IsSuccess")!.GetValue(raw)!;

        if (isSuccess)
        {
            var data = raw.GetType().GetProperty("Data")!.GetValue(raw);
            var instance = (TaleInstance)data!;

            if (instance.Status == TaleStatus.WaitingForInput)
            {
                return Accepted(Result<TaleInstanceDto>.Success(TaleInstanceDto.FromModel(instance)));
            }

            return Ok(Result<TaleInstanceDto>.Success(TaleInstanceDto.FromModel(instance)));
        }

        var err = (Error?)raw.GetType().GetProperty("Error")!.GetValue(raw);

        if (err is TalePausedError)
        {
            return Accepted(Result.Fail(err));
        }

        return BadRequest(Result.Fail(err ?? new Error { Message = "Unknown error" }));
    }
}

/// <summary>DTO for <see cref="TaleInstance"/> over HTTP.</summary>
public class TaleInstanceDto
{
    public string TaleId { get; set; } = default!;
    public string HandlerTypeName { get; set; } = default!;
    public TaleStatus Status { get; set; }
    public ChapterInfo? CurrentChapter { get; set; }
    public List<ChapterInfo> History { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    public static TaleInstanceDto FromModel(TaleInstance instance) => new()
    {
        TaleId = instance.TaleId.ToString(),
        HandlerTypeName = instance.HandlerTypeName,
        Status = instance.Status,
        CurrentChapter = instance.CurrentChapter,
        History = instance.History,
        CreatedAt = instance.CreatedAt,
        LastUpdatedAt = instance.LastUpdatedAt
    };
}

/// <summary>Result envelope returned by <c>GET /api/tale/{id}/result</c>.</summary>
public class TaleResultDto
{
    public string TaleId { get; set; } = default!;
    public TaleStatus Status { get; set; }
    public object? Output { get; set; }
}
