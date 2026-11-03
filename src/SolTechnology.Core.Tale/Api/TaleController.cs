using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;

namespace SolTechnology.Core.Tale.Api;

/// <summary>
/// Base REST API controller for the Tale Framework. Provides start/resume/cancel/query endpoints.
/// </summary>
/// <remarks>
/// Only handlers opted in with <c>ExposeOverHttp&lt;THandler&gt;()</c> are served — see
/// <see cref="TaleHandlerRegistry"/>.
/// Inherit and annotate with your auth attributes before exposing publicly.
/// </remarks>
[ApiController]
[Route("api/tale")]
public abstract partial class TaleController(
    TaleManager taleManager,
    TaleHandlerRegistry registry,
    TaleOptions options,
    ILogger<TaleController> logger)
    : ControllerBase
{
    /// <summary>Starts, resumes, cancels and reads tales.</summary>
    protected TaleManager TaleManager { get; } = taleManager;

    /// <summary>Handlers exposed over HTTP; others get a 404.</summary>
    protected TaleHandlerRegistry Registry { get; } = registry;

    /// <summary>The Tale options registered by <c>AddSolTale</c>.</summary>
    protected TaleOptions Options { get; } = options;

    /// <summary>Logger for request-level failures and rejected handlers.</summary>
    protected ILogger<TaleController> Logger { get; } = logger;

    /// <summary>Start a new tale with the given input.</summary>
    [HttpPost("{handlerTypeName}/start")]
    public virtual async Task<IActionResult> StartStory(
        [FromRoute] string handlerTypeName,
        [FromBody] JsonElement input,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting tale: {HandlerType}", handlerTypeName);
        if (!TryResolveHandler(handlerTypeName, out var handlerType, out var args, out var error))
        {
            return error;
        }

        return !TryReadInput(input, args[0], out var typedInput, out error)
            ? error
            : await Dispatch(
                () => TaleManagerInvoker.StartStory(TaleManager, handlerType, args, typedInput, idempotencyKey, cancellationToken),
                ex => Logger.LogError(ex, "Failed to start tale {HandlerType}", handlerTypeName));
    }

    /// <summary>Resume a paused tale with user input.</summary>
    [HttpPost("{taleId}")]
    public virtual async Task<IActionResult> ResumeStory(
        [FromRoute] string taleId,
        [FromBody] JsonElement? userInput = null,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Resuming tale: {TaleId}", taleId);
        if (!Auid.TryParse(taleId, null, out var parsedTaleId))
        {
            return InvalidTaleId(taleId);
        }

        var state = await TaleManager.GetStoryState(parsedTaleId, cancellationToken);
        if (state.IsFailure)
        {
            return NotFound(state);
        }

        return !TryResolveHandler(state.Data!.HandlerTypeName, out var handlerType, out var args, out var error)
            ? error
            : await Dispatch(
                () => TaleManagerInvoker.ResumeStory(TaleManager, handlerType, args, parsedTaleId, userInput, cancellationToken),
                ex => Logger.LogError(ex, "Failed to resume tale {TaleId}", taleId));
    }

    /// <summary>Get the current state of a tale.</summary>
    [HttpGet("{taleId}")]
    public virtual async Task<IActionResult> GetStoryState([FromRoute] string taleId)
    {
        if (!Auid.TryParse(taleId, null, out var parsedTaleId))
        {
            return InvalidTaleId(taleId);
        }

        var result = await TaleManager.GetStoryState(parsedTaleId, RequestAborted);
        return result.IsFailure
            ? NotFound(result)
            : Ok(Result<TaleInstanceDto>.Success(TaleInstanceDto.FromModel(result.Data!)));
    }

    /// <summary>Get the deserialized output of a completed tale.</summary>
    [HttpGet("{taleId}/result")]
    public virtual async Task<IActionResult> GetStoryResult([FromRoute] string taleId)
    {
        if (!Auid.TryParse(taleId, null, out var parsedTaleId))
        {
            return InvalidTaleId(taleId);
        }

        var state = await TaleManager.GetStoryState(parsedTaleId, RequestAborted);
        if (state.IsFailure)
        {
            return NotFound(state);
        }

        var instance = state.Data!;
        if (instance.Status != TaleStatus.Completed)
        {
            return BadRequest(Result.Fail($"Tale is not completed. Current status: {instance.Status}"));
        }

        return TryResolveHandler(instance.HandlerTypeName, out _, out var args, out var error)
            ? ReadOutput(instance, args[1], taleId)
            : error;
    }

    /// <summary>Cancel a running or paused tale.</summary>
    [HttpDelete("{taleId}")]
    public virtual async Task<IActionResult> CancelStory([FromRoute] string taleId)
    {
        if (!Auid.TryParse(taleId, null, out var parsedTaleId))
        {
            return InvalidTaleId(taleId);
        }

        var result = await TaleManager.CancelStory(parsedTaleId, RequestAborted);
        return result.IsFailure
            ? BadRequest(result)
            : Ok(Result<TaleInstanceDto>.Success(TaleInstanceDto.FromModel(result.Data!)));
    }
}
