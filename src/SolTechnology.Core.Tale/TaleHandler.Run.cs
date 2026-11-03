using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale;

public abstract partial class TaleHandler<TInput, TContext, TOutput>
{
    internal async Task<Result<TOutput>> Run(
        TContext context,
        JsonElement? resumeInput,
        TaleRunOptions run,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Tale [{Handler}] processing...", GetType().Name);

        var repository = run.Persist ? _serviceProvider.GetService<ITaleRepository>() : null;
        var options = _serviceProvider.GetService<TaleOptions>() ?? TaleOptions.Default;
        var timeProvider = _serviceProvider.GetService<TimeProvider>();
        _engine = new TaleEngine<TInput, TContext, TOutput>(_serviceProvider, _logger, repository, options, timeProvider);
        Context = context;

        _engine.Initialize(Context, GetType(), resumeInput, run, cancellationToken);

        using var _ = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["TaleHandler"] = GetType().Name,
            ["TaleId"] = Context.TaleInstanceId
        });

        try
        {
            var tale = Tell();
            await _engine.Run(tale.Steps);

            await SafePersistTerminalState();
            return _engine.GetResult(tale.Conclusion);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _engine.MarkCancelled();
            await SafePersistTerminalState();
            return Result<TOutput>.Fail(new TaleCancelledError(Context.TaleInstanceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tale [{Handler}] failed", GetType().Name);
            var error = UnexpectedFailure.Of($"Tale [{GetType().Name}] failed unexpectedly.");
            _engine.Fail(error);
            await SafePersistTerminalState();
            return Result<TOutput>.Fail(error);
        }
    }

    private async Task SafePersistTerminalState()
    {
        try
        {
            await _engine.PersistTerminalState();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist terminal tale state for [{TaleId}]", Context.TaleInstanceId);
        }
    }
}
