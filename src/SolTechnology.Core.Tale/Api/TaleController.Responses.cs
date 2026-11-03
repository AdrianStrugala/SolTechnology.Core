using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Api;

public abstract partial class TaleController
{
    private CancellationToken RequestAborted => HttpContext?.RequestAborted ?? default;

    private bool TryResolveHandler(string name, out Type handlerType, out Type[] args, out IActionResult error)
    {
        args = [];
        error = null!;
        if (!Registry.TryResolve(name, out handlerType))
        {
            if (Registry.TryResolveScanned(name, out var scanned))
            {
                Logger.LogWarning(
                    "Tale handler [{HandlerName}] exists but is not exposed over HTTP; call ExposeOverHttp<THandler>() to expose it.",
                    scanned.Name);
            }

            error = NotFound(Result.Fail("Tale handler not found."));
            return false;
        }

        if (!TaleManagerInvoker.TryGetGenericArgs(handlerType, out args))
        {
            error = new BadRequestObjectResult(Result.Fail($"Type '{handlerType.Name}' is not a TaleHandler"));
            return false;
        }

        return true;
    }

    // A tale of a handler not exposed over HTTP answers exactly like a missing tale.
    private async Task<Result<TaleInstance>> FindExposedTale(Auid taleId)
    {
        var state = await TaleManager.GetStoryState(taleId, RequestAborted);
        return state.IsSuccess && !Registry.TryResolve(state.Data!.HandlerTypeName, out _)
            ? Result<TaleInstance>.Fail("Tale not found")
            : state;
    }

    private bool TryReadInput(JsonElement input, Type inputType, out object typedInput, out IActionResult error)
    {
        typedInput = null!;
        error = null!;
        try
        {
            typedInput = JsonSerializer.Deserialize(input.GetRawText(), inputType, TaleJsonOptions.Default)!;
        }
        catch (JsonException ex)
        {
            error = BadRequest(Result.Fail($"Failed to deserialize input: {ex.Message}"));
            return false;
        }

        if (typedInput == null)
        {
            error = BadRequest(Result.Fail("Failed to deserialize input"));
            return false;
        }

        return true;
    }

    private async Task<IActionResult> Dispatch(Func<Task<Result<TaleInstance>>> call, Action<Exception> logFailure)
    {
        try
        {
            return ToActionResult(await call());
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            logFailure(tie.InnerException);
            return UnexpectedFailure();
        }
        catch (Exception ex)
        {
            logFailure(ex);
            return UnexpectedFailure();
        }
    }

    private IActionResult ReadOutput(TaleInstance instance, Type contextType, string taleId)
    {
        try
        {
            var context = JsonSerializer.Deserialize(instance.Context, contextType, TaleJsonOptions.Default);
            return Ok(new TaleResultDto
            {
                TaleId = instance.TaleId.ToString(),
                Status = instance.Status,
                Output = contextType.GetProperty("Output")?.GetValue(context)
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to extract result for tale [{TaleId}]", taleId);
            return UnexpectedFailure();
        }
    }

    private IActionResult ToActionResult(Result<TaleInstance> result)
    {
        if (result.IsSuccess)
        {
            var dto = Result<TaleInstanceDto>.Success(TaleInstanceDto.FromModel(result.Data!));
            return result.Data!.Status == TaleStatus.WaitingForInput ? Accepted(dto) : Ok(dto);
        }

        return result.Error is TalePausedError
            ? Accepted(Result.Fail(result.Error))
            : BadRequest(Result.Fail(result.Error ?? new Error { Message = "Unknown error" }));
    }

    private BadRequestObjectResult InvalidTaleId(string taleId) =>
        BadRequest(Result.Fail($"Invalid tale ID format: {taleId}"));

    private ObjectResult UnexpectedFailure() =>
        StatusCode(500, Result.Fail(new Error
        {
            Message = "An unexpected error occurred.",
            CorrelationId = Activity.Current?.TraceId.ToHexString() ?? HttpContext?.TraceIdentifier
        }));
}
