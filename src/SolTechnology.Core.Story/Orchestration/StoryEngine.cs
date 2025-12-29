using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Orchestration;

/// <summary>
/// JSON serialization options used throughout the Story framework for consistent serialization.
/// </summary>
internal static class StoryJsonOptions
{
    public static JsonSerializerOptions Default => new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };
}

/// <summary>
/// Internal execution engine for Story Framework.
/// Manages chapter execution, state persistence, and error handling.
/// </summary>
/// <typeparam name="TContext">The context type carrying state through the story</typeparam>
internal class StoryEngine<TContext>(
    IServiceProvider serviceProvider,
    ILogger logger,
    IStoryRepository? repository,
    bool stopOnFirstError = true)
    where TContext : class
{
    private TContext _context = null!;
    private CancellationToken _cancellationToken;
    private JsonElement? _resumeInput;
    
    private readonly List<Error> _errors = new();
    private readonly List<ChapterInfo> _chapterHistory = new();
    
    private bool _isPaused;
    private bool _hasFailed;
    private bool _isInitialized;
    private string _handlerTypeName = string.Empty;

    /// <summary>
    /// Initializes the story engine with context and optional resume data.
    /// </summary>
    /// <param name="context">The story context</param>
    /// <param name="handlerTypeName">Name of the handler for logging</param>
    /// <param name="resumeInput">Optional input for resuming an interactive chapter</param>
    /// <param name="cancellationToken">Cancellation token for the execution</param>
    public async Task Initialize(
        TContext context,
        string handlerTypeName,
        JsonElement? resumeInput,
        CancellationToken cancellationToken)
    {
        _context = context;
        _handlerTypeName = handlerTypeName;
        _resumeInput = resumeInput;
        _cancellationToken = cancellationToken;
        _isInitialized = true;

        dynamic contextBase = _context;
        
        Auid storyId = Auid.Empty;
        try 
        {
            storyId = contextBase.StoryInstanceId;
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            // Jeśli właściwość nie istnieje, traktujemy jako Empty
        }

        if (repository != null && storyId != Auid.Empty)
        {
            await LoadStoryState(storyId);
            logger.LogInformation("Story {StoryId} resuming from saved state", storyId);
        }
        else if (repository != null)
        {
            var newId = Auid.New("STR");
            contextBase.StoryInstanceId = newId;
            logger.LogInformation("Story started with ID {StoryId}", newId);
        }
    }

    /// <summary>
    /// Executes a chapter in the story. Handles interactive chapters, resumption, and state persistence.
    /// </summary>
    /// <typeparam name="TChapter">The chapter type to execute</typeparam>
    /// <param name="context">The story context</param>
    public async Task ExecuteChapter<TChapter>(TContext context)
        where TChapter : IChapter<TContext>
    {
        if (!_isInitialized)
            throw new InvalidOperationException("StoryEngine must be initialized first");

        if ((_isPaused && _resumeInput == null) || (_hasFailed && stopOnFirstError))
        {
            return;
        }

        var chapter = serviceProvider.GetService<TChapter>();
        if (chapter == null)
        {
            HandleChapterFailure(null, new Error { Message = $"Chapter {typeof(TChapter).Name} not registered in DI container" });
            return;
        }

        var existingChapter = _chapterHistory.FirstOrDefault(h => h.ChapterId == chapter.ChapterId);
        if (existingChapter is { Status: StoryStatus.Completed })
        {
            return;
        }

        // Clear pause flag ONLY if this is the chapter waiting for input
        if (_isPaused && existingChapter?.Status == StoryStatus.WaitingForInput && existingChapter?.ChapterId == chapter.ChapterId)
        {
            logger.LogInformation("Resuming story at chapter {ChapterId}", chapter.ChapterId);
            _isPaused = false;
        }

        var chapterInfo = existingChapter ?? new ChapterInfo
        {
            ChapterId = chapter.ChapterId,
            StartedAt = DateTime.UtcNow,
            Status = StoryStatus.Running
        };

        if (existingChapter == null) _chapterHistory.Add(chapterInfo);

        logger.LogInformation("Executing chapter: {ChapterId}", chapter.ChapterId);

        try
        {
            _cancellationToken.ThrowIfCancellationRequested();
            Result result;

            var isInteractive = typeof(TChapter).BaseType?.IsGenericType == true &&
                                typeof(TChapter).BaseType?.GetGenericTypeDefinition() == typeof(InteractiveChapter<,>);

            if (isInteractive)
            {
                result = await ExecuteInteractiveChapter(chapter, context, chapterInfo);
            }
            else
            {
                result = await chapter.Read(context);
            }

            if (result.IsFailure)
            {
                HandleChapterFailure(chapterInfo, result.Error!);
            }
            else if (chapterInfo.Status != StoryStatus.WaitingForInput)
            {
                chapterInfo.Status = StoryStatus.Completed;
                chapterInfo.FinishedAt = DateTime.UtcNow;
            }
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation to be handled by StoryHandler
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chapter {ChapterId} failed", chapter.ChapterId);
            HandleChapterFailure(chapterInfo, Error.From(ex));
        }

        if (repository != null) await SaveStoryState();
    }

    private async Task<Result> ExecuteInteractiveChapter(
        object chapter,
        TContext context,
        ChapterInfo chapterInfo)
    {
        var chapterType = chapter.GetType();

        var getSchemaMethod = chapterType.GetMethod("GetRequiredInputSchema");
        if (getSchemaMethod != null)
        {
            chapterInfo.RequiredData = getSchemaMethod.Invoke(chapter, null) as List<DataField> ?? new();
        }

        if (_resumeInput == null)
        {
            _isPaused = true;
            chapterInfo.Status = StoryStatus.WaitingForInput;

            dynamic baseContext = _context;

            try
            {
                var idProp = chapterType.GetProperty("ChapterId");
                var chapterId = idProp?.GetValue(chapter) as string;
                baseContext.CurrentChapterId = chapterId;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                // Ignorujemy, jeśli kontekst nie ma właściwości CurrentChapterId
            }

            return Result.Success();
        }

        var inputType = chapterType.BaseType!.GetGenericArguments()[1];
        var deserializedInput = JsonSerializer.Deserialize(_resumeInput.Value, inputType, StoryJsonOptions.Default);

        chapterInfo.ProvidedData = _resumeInput;

        var executeMethod = chapterType.GetMethod("ReadWithInput");
        var task = executeMethod!.Invoke(chapter, [context, deserializedInput]) as Task<Result>;
        var result = await task!;

        if (result.IsSuccess)
        {
            _resumeInput = null;
            // Reset status from WaitingForInput to Running so it can be marked as Completed
            chapterInfo.Status = StoryStatus.Running;
        }

        return result;
    }

    private void HandleChapterFailure(ChapterInfo? chapterInfo, Error error)
    {
        _errors.Add(error);
        _hasFailed = true;
        if (chapterInfo != null)
        {
            chapterInfo.Error = error;
            chapterInfo.Status = StoryStatus.Failed;
            chapterInfo.FinishedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets the final result of the story execution.
    /// Returns success with output if completed, or failure if errors occurred or story is paused.
    /// </summary>
    /// <typeparam name="TOutput">The output type</typeparam>
    /// <returns>Result containing output or error</returns>
    public Result<TOutput> GetResult<TOutput>() where TOutput : class, new()
    {
        if (_errors.Any())
        {
            // Return single error if only one, otherwise aggregate
            return _errors.Count == 1
                ? Result<TOutput>.Fail(_errors[0])
                : Result<TOutput>.Fail(new AggregateError(_errors));
        }

        if (_isPaused)
        {
            return Result<TOutput>.Fail(new Error
            {
                Message = "Story paused waiting for user input"
            });
        }

        // FIX: Rzutowanie na dynamic w nawiasach jest bezpieczne
        dynamic contextBase = _context;
        try
        {
            return Result<TOutput>.Success((TOutput)contextBase.Output);
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            return Result<TOutput>.Fail("Could not retrieve Output from Context");
        }
    }

    private async Task SaveStoryState()
    {
        if (repository == null) return;
        
        dynamic contextBase = _context;
        Auid storyId = contextBase.StoryInstanceId;

        var instance = new StoryInstance
        {
            StoryId = storyId,
            HandlerTypeName = _handlerTypeName,
            Status = _isPaused ? StoryStatus.WaitingForInput : _hasFailed ? StoryStatus.Failed : StoryStatus.Running,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = new List<ChapterInfo>(_chapterHistory),
            CurrentChapter = _isPaused ? _chapterHistory.LastOrDefault() : null,
            Context = JsonSerializer.Serialize(_context, StoryJsonOptions.Default)
        };

        await repository.SaveAsync(instance);
    }

    private async Task LoadStoryState(Auid storyId)
    {
        if (repository == null) return;
        var instance = await repository.FindById(storyId);
        if (instance == null) return;

        _chapterHistory.Clear();
        _chapterHistory.AddRange(instance.History);
        
        _isPaused = instance.Status == StoryStatus.WaitingForInput;
        _hasFailed = instance.Status == StoryStatus.Failed;
    }
}