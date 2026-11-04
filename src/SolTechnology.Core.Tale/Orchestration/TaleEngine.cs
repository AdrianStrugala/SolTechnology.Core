using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Internal execution engine for a tale. Strongly typed — no <c>dynamic</c>, no string-matched
/// pause detection. Runs the plan's chapters, manages pause/resume, aggregates errors and, for
/// <see cref="TaleManager"/> runs, persists state and renews the Running lease while steps execute.
/// </summary>
internal sealed class TaleEngine<TInput, TContext, TOutput>(
    IServiceProvider serviceProvider,
    ILogger logger,
    ITaleRepository? repository,
    TaleOptions? options,
    TimeProvider? timeProvider = null)
    where TInput : class
    where TContext : Context<TInput, TOutput>, new()
    where TOutput : class, new()
{
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly TaleRunState _state = new(timeProvider ?? TimeProvider.System);
    private TaleStatePersistence? _persistence;
    private TaleLeaseHeartbeat? _heartbeat;
    private ChapterReader<TInput, TContext, TOutput> _reader = null!;
    private TContext _context = null!;
    private CancellationToken _cancellationToken;
    private CancellationToken _chapterToken;
    private string _handlerTypeName = string.Empty;

    public void Initialize(
        TContext context,
        Type handlerType,
        JsonElement? resumeInput,
        TaleRunOptions run,
        CancellationToken cancellationToken)
    {
        _context = context;
        _handlerTypeName = handlerType.Name;
        _state.ResumeInput = resumeInput;
        _cancellationToken = cancellationToken;
        _persistence = repository == null
            ? null
            : new(repository, _state, RunningLeaseTimeout, serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                serviceProvider.GetService<LeaseReadBackCheck>(), logger);
        _reader = new(logger, _state, new(logger, _state, _handlerTypeName, isPersisted: _persistence != null));

        if (run.Resume != null)
        {
            _persistence?.Restore(run.Resume);
            logger.LogInformation("Tale [{TaleId}] resuming from saved state", context.TaleInstanceId);
            return;
        }

        _persistence?.Start(run.IdempotencyKey);
        var newId = Auid.New((options ?? TaleOptions.Default).TaleIdPrefix);
        context.TaleInstanceId = newId;
        logger.LogInformation("Tale started with ID [{TaleId}]", newId);
    }

    public async Task Run(IReadOnlyList<TaleStep> steps)
    {
        // The first save claims the tale (and a new tale's idempotency key) before any chapter runs.
        await Save(TaleStatus.Running, _cancellationToken);
        await using var heartbeat = _persistence == null || _state.SaveError != null ? null : StartHeartbeat(_persistence);
        _heartbeat = heartbeat;
        _chapterToken = heartbeat?.Token ?? _cancellationToken;
        await new TaleStepInterpreter(_state, ExecuteChapter).Run(steps, _context);
    }

    public Result<TOutput> GetResult(Func<object, TOutput> conclusion) => _state switch
    {
        { SaveError: { } saveError } => Result<TOutput>.Fail(saveError),
        { IsCancelled: true } => Result<TOutput>.Fail(new TaleCancelledError(_context.TaleInstanceId)),
        { Errors.Count: > 0 } => Result<TOutput>.Fail(_state.Errors[0]),
        { IsPaused: true } => Result<TOutput>.Fail(new TalePausedError(_context.TaleInstanceId, _context.CurrentChapterId)),
        _ => Result<TOutput>.Success(conclusion(_context))
    };

    public void MarkCancelled() => _state.IsCancelled = true;

    public void Fail(Error error) => _state.Fail(null, error);

    // The terminal state must be stored even when the run's token is what ended the run.
    public Task PersistTerminalState() =>
        _state.SaveError == null ? Save(_state.TerminalStatus, CancellationToken.None) : Task.CompletedTask;

    private async Task ExecuteChapter(Type chapterType, int stepIndex)
    {
        if (_state.IsCancelled || _state.SaveError != null || _state.HasFailed
            || (_state.IsPaused && _state.ResumeInput == null))
        {
            return;
        }

        if (serviceProvider.GetService(chapterType) is not IChapter<TContext> chapter)
        {
            _state.Fail(null, new Error { Message = $"Chapter {chapterType.Name} not registered in DI container" });
            return;
        }

        var chapterInfo = _state.Track(chapter.ChapterId, ChapterTypeName.Of(chapterType), stepIndex, out var resumed);
        if (chapterInfo == null)
        {
            return;
        }

        if (resumed)
        {
            logger.LogInformation("Resuming tale at chapter [{ChapterId}]", chapter.ChapterId);
        }

        logger.LogInformation("Executing chapter: [{ChapterId}]", chapter.ChapterId);
        _heartbeat?.EnterChapter(chapter.ChapterId);
        await _reader.Read(chapter, chapterInfo, _context, _chapterToken, _cancellationToken);
        _heartbeat?.LeaveChapter();
        await Save(_state.IntermediateStatus, _cancellationToken);
    }

    private TimeSpan RunningLeaseTimeout => (options ?? TaleOptions.Default).RunningLeaseTimeout;

    private TaleLeaseHeartbeat StartHeartbeat(TaleStatePersistence persistence) =>
        new TaleLeaseHeartbeat(persistence, logger, _context.TaleInstanceId, _cancellationToken)
            .Start(_clock, RunningLeaseTimeout);

    private Task Save(TaleStatus status, CancellationToken cancellationToken) =>
        _persistence?.Save(_context, _context.TaleInstanceId, _handlerTypeName, status, cancellationToken)
        ?? Task.CompletedTask;
}
