using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;

namespace SolTechnology.Core.Tale.Orchestration;

public partial class TaleManager
{
    private async Task<Result<TaleInstance>> ExecuteTalePipeline<THandler, TInput, TContext, TOutput>(
        TInput? input,
        JsonElement? userInput,
        Auid taleId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
        where THandler : TaleHandler<TInput, TContext, TOutput>
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
    {
        // Dedicated DI scope for this invocation — avoids captive dependencies (Scoped DbContext etc.).
        using var scope = scopeFactory.CreateScope();

        try
        {
            var prepared = taleId == Auid.Empty
                ? NewRun<TInput, TContext, TOutput>(input)
                : await new ResumableTaleLoader(repository).Load<TInput, TContext, TOutput>(taleId, cancellationToken);
            if (prepared.IsFailure)
            {
                return Result<TaleInstance>.Fail(prepared.Error!);
            }

            var (context, existing) = prepared.Data;
            var handler = ActivatorUtilities.CreateInstance<THandler>(scope.ServiceProvider);
            var run = new TaleRunOptions { Persist = true, Resume = existing, IdempotencyKey = idempotencyKey };
            var result = await handler.Run(context, userInput, run, cancellationToken);
            return await ToInstance(typeof(THandler), result, context.TaleInstanceId, idempotencyKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing tale {Handler}", typeof(THandler).Name);
            return Result<TaleInstance>.Fail($"Tale {typeof(THandler).Name} failed unexpectedly.");
        }
    }

    private static Result<(TContext Context, TaleInstance? Instance)> NewRun<TInput, TContext, TOutput>(TInput? input)
        where TInput : class
        where TContext : Context<TInput, TOutput>, new()
        where TOutput : class, new()
        => input == null
            ? Result<(TContext, TaleInstance?)>.Fail("Input is required to start a tale")
            : Result<(TContext, TaleInstance?)>.Success((new TContext { Input = input }, null));

    private async Task<Result<TaleInstance>> ToInstance(
        Type handlerType, Result result, Auid activeId, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (result.Error is ConflictError && !string.IsNullOrEmpty(idempotencyKey))
        {
            var winner = await repository.FindByIdempotencyKey(idempotencyKey, cancellationToken);
            if (winner != null)
            {
                return Result<TaleInstance>.Success(winner);
            }
        }

        if (result.Error is TalePausedError)
        {
            var paused = await repository.FindById(activeId, cancellationToken);
            return paused != null
                ? Result<TaleInstance>.Success(paused)
                : Result<TaleInstance>.Fail("Instance not found after execution");
        }

        if (result.IsFailure)
        {
            return Result<TaleInstance>.Fail(result.Error!);
        }

        // A run without a repository write still completed; report it without a stored record.
        return Result<TaleInstance>.Success(await repository.FindById(activeId, cancellationToken) ?? new TaleInstance
        {
            TaleId = activeId,
            HandlerTypeName = handlerType.Name,
            Status = TaleStatus.Completed,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            LastUpdatedAt = _timeProvider.GetUtcNow().UtcDateTime
        });
    }
}
