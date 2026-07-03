using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;
// collapsed: using SolTechnology.Core.Tale.Tale;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// JSON serialization options shared across the Tale framework.
/// </summary>
internal static class TaleJsonOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        // Tale Context may carry EF/domain entities with bidirectional navs.
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
}

/// <summary>
/// Internal execution engine for a tale. Strongly typed — no <c>dynamic</c>, no string-matched
/// pause detection. Tracks chapter execution, manages pause/resume, aggregates errors and
/// (optionally) persists state.
/// </summary>
internal sealed class TaleEngine<TInput, TContext, TOutput>
    where TInput : class
    where TContext : Context<TInput, TOutput>, new()
    where TOutput : class, new()
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly ITaleRepository? _repository;
    private readonly TaleOptions _options;
    private readonly TimeProvider _timeProvider;

    private TContext _context = null!;
    private CancellationToken _cancellationToken;
    private JsonElement? _resumeInput;
    private DateTime _createdAt;

    private readonly List<Error> _errors = new();
    private readonly List<ChapterInfo> _chapterHistory = new();

    private bool _isPaused;
    private bool _hasFailed;
    private bool _isCancelled;
    private bool _isInitialized;
    private string _handlerTypeName = string.Empty;

    public TaleEngine(
        IServiceProvider serviceProvider,
        ILogger logger,
        ITaleRepository? repository,
        TaleOptions? options,
        TimeProvider? timeProvider = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _repository = repository;
        _options = options ?? TaleOptions.Default;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool IsPaused => _isPaused;
    public bool HasFailed => _hasFailed;
    public bool IsCancelled => _isCancelled;
    public IReadOnlyList<Error> Errors => _errors;
    public IReadOnlyList<ChapterInfo> History => _chapterHistory;
    public Auid TaleId => _context is null ? Auid.Empty : _context.TaleInstanceId;

    public async Task<Result> Initialize(
        TContext context,
        Type handlerType,
        JsonElement? resumeInput,
        CancellationToken cancellationToken)
    {
        _context = context;
        _handlerTypeName = handlerType.Name;
        _resumeInput = resumeInput;
        _cancellationToken = cancellationToken;
        _isInitialized = true;

        if (_repository != null && context.TaleInstanceId != Auid.Empty)
        {
            var loaded = await LoadTaleState(context.TaleInstanceId);
            if (loaded.IsFailure) return loaded;
            _logger.LogInformation("Tale {TaleId} resuming from saved state", context.TaleInstanceId);
        }
        else
        {
            _createdAt = _timeProvider.GetUtcNow().UtcDateTime;
            if (_repository != null)
            {
                var newId = Auid.New(_options.TaleIdPrefix);
                context.TaleInstanceId = newId;
                _logger.LogInformation("Tale started with ID {TaleId}", newId);
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Interprets a <see cref="Tale{TOutput}"/> plan: walks each recorded step in order, applying the
    /// first-error short-circuit. Chapters after a failure are skipped; an <c>Otherwise</c> step can
    /// clear the failure and resume.
    /// </summary>
    public async Task Run(IReadOnlyList<TaleStep> steps)
    {
        foreach (var step in steps)
        {
            switch (step)
            {
                case ReadStep s:
                    await ExecuteChapter(s.ChapterType);
                    break;
                case GuardStep s:
                    ApplyGuard(s);
                    break;
                case FallbackChapterStep s:
                    await ApplyFallbackChapter(s.ChapterType);
                    break;
                case FallbackStep s:
                    await ApplyFallback(s.Recover);
                    break;
                case OnLostStep s:
                    await ApplyOnLost(s.Effect);
                    break;
                case OnWonStep s:
                    await ApplyOnWon(s.Effect);
                    break;
                case InlineStep s:
                    await ApplyInline(s.Action);
                    break;
            }
        }
    }

    // The tale runs on one of two tracks. Won-track steps act only while it is still succeeding;
    // lost-track steps act only after a failure (until an Otherwise recovers). Both tracks are
    // suspended once the tale is cancelled or paused.
    private bool OnWonTrack => !_isCancelled && !_isPaused && !_hasFailed;
    private bool OnLostTrack => !_isCancelled && !_isPaused && _hasFailed;

    private void ApplyGuard(GuardStep step)
    {
        if (!OnWonTrack) return;
        if (!step.Predicate(_context)) HandleChapterFailure(null, step.Error);
    }

    private async Task ApplyFallbackChapter(Type chapterType)
    {
        if (!OnLostTrack) return;
        ClearFailure();
        await ExecuteChapter(chapterType);
    }

    private async Task ApplyFallback(Func<object, Task<Result>> recover)
    {
        if (!OnLostTrack) return;
        ClearFailure();
        var result = await recover(_context);
        if (result.IsFailure) HandleChapterFailure(null, result.Error!);
    }

    private async Task ApplyOnLost(Func<Error, Task> effect)
    {
        if (!OnLostTrack) return;
        await effect(_errors[^1]);
    }

    private async Task ApplyOnWon(Func<object, Task> effect)
    {
        if (!OnWonTrack) return;
        await effect(_context);
    }

    private async Task ApplyInline(Func<object, Task<Result>> action)
    {
        if (!OnWonTrack) return;
        var result = await action(_context);
        if (result.IsFailure) HandleChapterFailure(null, result.Error!);
    }

    private void ClearFailure()
    {
        _hasFailed = false;
        _errors.Clear();
    }

    private async Task ExecuteChapter(Type chapterType)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("TaleEngine must be initialized first");

        if (_isCancelled) return;
        if (_isPaused && _resumeInput == null) return;
        if (_hasFailed) return;

        var chapter = _serviceProvider.GetService(chapterType) as IChapter<TContext>;
        if (chapter == null)
        {
            HandleChapterFailure(null, new Error
            {
                Message = $"Chapter {chapterType.Name} not registered in DI container"
            });
            return;
        }


        var existingChapter = _chapterHistory.FirstOrDefault(h => h.ChapterId == chapter.ChapterId);
        if (existingChapter is { Status: TaleStatus.Completed })
        {
            return;
        }

        if (_isPaused && existingChapter?.Status == TaleStatus.WaitingForInput &&
            existingChapter.ChapterId == chapter.ChapterId)
        {
            _logger.LogInformation("Resuming tale at chapter {ChapterId}", chapter.ChapterId);
            _isPaused = false;
        }

        var chapterInfo = existingChapter ?? new ChapterInfo
        {
            ChapterId = chapter.ChapterId,
            StartedAt = _timeProvider.GetUtcNow().UtcDateTime,
            Status = TaleStatus.Running
        };

        if (existingChapter == null) _chapterHistory.Add(chapterInfo);

        _logger.LogInformation("Executing chapter: {ChapterId}", chapter.ChapterId);

        try
        {
            _cancellationToken.ThrowIfCancellationRequested();

            Result result;
            if (TryGetInteractiveInputType(chapter.GetType(), out var inputType))
            {
                result = await ExecuteInteractiveChapter(chapter, inputType!, chapterInfo);
            }
            else
            {
                result = await chapter.Read(_context);
            }

            if (result.IsFailure)
            {
                // When the interactive chapter's ReadWithInput returns a validation error,
                // ExecuteInteractiveChapter re-pauses the tale (_isPaused = true) so the
                // user can retry. In that case, skip HandleChapterFailure — the tale is
                // NOT terminally failed. For all other failures (deserialization errors,
                // automated chapters), _isPaused stays false → terminal failure.
                if (!_isPaused)
                {
                    HandleChapterFailure(chapterInfo, result.Error!);
                }
            }
            else if (chapterInfo.Status != TaleStatus.WaitingForInput)
            {
                chapterInfo.Status = TaleStatus.Completed;
                chapterInfo.FinishedAt = _timeProvider.GetUtcNow().UtcDateTime;
            }
        }
        catch (OperationCanceledException)
        {
            _isCancelled = true;
            chapterInfo.Status = TaleStatus.Cancelled;
            chapterInfo.FinishedAt = _timeProvider.GetUtcNow().UtcDateTime;
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chapter {ChapterId} failed", chapter.ChapterId);
            HandleChapterFailure(chapterInfo, Error.From(ex));
        }

        if (_repository != null) await SaveTaleState(terminal: false);
    }

    /// <summary>
    /// Walks the type hierarchy looking for <see cref="InteractiveChapter{TContext,TChapterInput}"/>.
    /// Supports multi-level inheritance.
    /// </summary>
    private static bool TryGetInteractiveInputType(Type chapterType, out Type? inputType)
    {
        var t = chapterType;
        while (t != null && t != typeof(object))
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(InteractiveChapter<,>))
            {
                inputType = t.GetGenericArguments()[1];
                return true;
            }
            t = t.BaseType;
        }
        inputType = null;
        return false;
    }

    private async Task<Result> ExecuteInteractiveChapter(
        IChapter<TContext> chapter,
        Type inputType,
        ChapterInfo chapterInfo)
    {
        var chapterType = chapter.GetType();

        var schema = InteractiveChapterReflection.GetSchema(chapterType, chapter);
        if (schema != null)
        {
            chapterInfo.RequiredData = schema.ToList();
        }

        if (_resumeInput == null)
        {
            _isPaused = true;
            chapterInfo.Status = TaleStatus.WaitingForInput;
            _context.CurrentChapterId = chapter.ChapterId;
            return Result.Success();
        }

        object? deserializedInput;
        try
        {
            deserializedInput = JsonSerializer.Deserialize(_resumeInput.Value, inputType, TaleJsonOptions.Default);
        }
        catch (JsonException ex)
        {
            return Result.Fail(new Error
            {
                Message = $"Failed to deserialize input for chapter '{chapter.ChapterId}': {ex.Message}",
                Description = ex.ToString()
            });
        }

        if (deserializedInput == null)
        {
            return Result.Fail($"Input for chapter '{chapter.ChapterId}' deserialized to null.");
        }

        chapterInfo.ProvidedData = _resumeInput;

        var result = await InteractiveChapterReflection.InvokeReadWithInput(
            chapterType, chapter, _context, deserializedInput);

        if (result.IsSuccess)
        {
            _resumeInput = null;
            chapterInfo.Status = TaleStatus.Running;
            _context.CurrentChapterId = null;
        }
        else
        {
            PauseForRetry(chapter, chapterInfo, result.Error);
        }

        return result;
    }

    /// <summary>
    /// Keeps the tale paused at the current interactive chapter after a validation failure,
    /// allowing the user to retry with corrected input. Does NOT set <see cref="_hasFailed"/>
    /// — the tale is still alive and waiting for input.
    /// </summary>
    private void PauseForRetry(IChapter<TContext> chapter, ChapterInfo chapterInfo, Error? error)
    {
        _logger.LogInformation("Interactive chapter {ChapterId} rejected input: {Error}",
            chapter.ChapterId, error?.Message);
        _resumeInput = null;
        _isPaused = true;
        chapterInfo.Status = TaleStatus.WaitingForInput;
        chapterInfo.Error = error;
    }

    private void HandleChapterFailure(ChapterInfo? chapterInfo, Error error)
    {
        _errors.Add(error);
        _hasFailed = true;
        if (chapterInfo != null)
        {
            chapterInfo.Error = error;
            chapterInfo.Status = TaleStatus.Failed;
            chapterInfo.FinishedAt = _timeProvider.GetUtcNow().UtcDateTime;
        }
    }

    public Result<TOutput> GetResult(Func<object, TOutput> conclusion)
    {
        if (_isCancelled)
            return Result<TOutput>.Fail(new TaleCancelledError(_context is null ? Auid.Empty : _context.TaleInstanceId));

        if (_errors.Count > 0)
            return Result<TOutput>.Fail(_errors[0]);

        if (_isPaused)
            return Result<TOutput>.Fail(new TalePausedError(_context.TaleInstanceId, _context.CurrentChapterId));

        return Result<TOutput>.Success(conclusion(_context));
    }

    public TaleStatus GetTerminalStatus()
    {
        if (_isCancelled) return TaleStatus.Cancelled;
        if (_hasFailed) return TaleStatus.Failed;
        if (_isPaused) return TaleStatus.WaitingForInput;
        return TaleStatus.Completed;
    }

    public Task PersistTerminalState()
    {
        if (_repository == null) return Task.CompletedTask;
        return SaveTaleState(terminal: true);
    }

    private async Task SaveTaleState(bool terminal)
    {
        if (_repository == null) return;
        if (_context.TaleInstanceId == Auid.Empty) return;

        var instance = new TaleInstance
        {
            TaleId = _context.TaleInstanceId,
            HandlerTypeName = _handlerTypeName,
            Status = terminal ? GetTerminalStatus() : IntermediateStatus(),
            CreatedAt = _createdAt == default ? _timeProvider.GetUtcNow().UtcDateTime : _createdAt,
            LastUpdatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            History = new List<ChapterInfo>(_chapterHistory),
            CurrentChapter = _isPaused ? _chapterHistory.LastOrDefault() : null,
            Context = JsonSerializer.Serialize(_context, TaleJsonOptions.Default)
        };

        await _repository.SaveAsync(instance);
    }

    private TaleStatus IntermediateStatus()
    {
        if (_isCancelled) return TaleStatus.Cancelled;
        if (_isPaused) return TaleStatus.WaitingForInput;
        if (_hasFailed) return TaleStatus.Failed;
        return TaleStatus.Running;
    }

    private async Task<Result> LoadTaleState(Auid taleId)
    {
        if (_repository == null) return Result.Success();

        var instance = await _repository.FindById(taleId);
        if (instance == null) return Result.Success();


        _chapterHistory.Clear();
        _chapterHistory.AddRange(instance.History);
        _createdAt = instance.CreatedAt;
        _isPaused = instance.Status == TaleStatus.WaitingForInput;
        _hasFailed = instance.Status == TaleStatus.Failed;
        _isCancelled = instance.Status == TaleStatus.Cancelled;

        return Result.Success();
    }
}

/// <summary>
/// Reflection helpers for interactive chapters. Method handles are cached for performance.
/// </summary>
internal static class InteractiveChapterReflection
{
    private static readonly ConcurrentDictionary<Type, MethodInfo?> _getSchemaCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo?> _readWithInputCache = new();

    public static IReadOnlyList<DataField>? GetSchema(Type chapterType, object chapterInstance)
    {
        var method = _getSchemaCache.GetOrAdd(chapterType, t =>
            t.GetMethod("GetRequiredInputSchema", BindingFlags.Public | BindingFlags.Instance));
        if (method == null) return null;

        var result = method.Invoke(chapterInstance, null);
        return result switch
        {
            IReadOnlyList<DataField> ro => ro,
            IEnumerable<DataField> list => list.ToList(),
            _ => null
        };
    }

    public static async Task<Result> InvokeReadWithInput(
        Type chapterType, object chapterInstance, object context, object input)
    {
        var method = _readWithInputCache.GetOrAdd(chapterType, t =>
            t.GetMethod("ReadWithInput", BindingFlags.Public | BindingFlags.Instance));
        if (method == null)
        {
            return Result.Fail($"ReadWithInput not found on {chapterType.Name}");
        }

        var task = (Task<Result>)method.Invoke(chapterInstance, new[] { context, input })!;
        return await task;
    }
}

