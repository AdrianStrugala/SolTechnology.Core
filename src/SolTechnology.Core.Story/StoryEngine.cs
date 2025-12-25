using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Story.Models;

namespace SolTechnology.Core.Story;

/// <summary>
/// Shared JSON serializer options for story context persistence.
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
/// Internal orchestration engine for story execution.
/// Handles chapter execution, error aggregation, pause/resume, and persistence.
/// Not exposed to users - accessed through StoryHandler.
/// </summary>
internal class StoryEngine
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly StoryOptions _options;
    private readonly List<Error> _errors = new();
    private CancellationToken _cancellationToken;
    private object _narration = null!;
    private string? _resumeFromChapterId;
    private JsonElement? _chapterInput;
    private bool _isPaused;
    private bool _hasFailed;
    private readonly List<ChapterInfo> _chapterHistory = new();
    private bool _isInitialized;
    private string _handlerTypeName = string.Empty;

    public StoryEngine(
        IServiceProvider serviceProvider,
        ILogger logger,
        StoryOptions options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Initialize the engine with narration and cancellation token.
    /// Must be called before executing any chapters.
    /// </summary>
    public async Task Initialize<TInput, TOutput>(
        Narration<TInput, TOutput> narration,
        string handlerTypeName,
        CancellationToken cancellationToken)
        where TInput : class
        where TOutput : class, new()
    {
        _narration = narration;
        _handlerTypeName = handlerTypeName;
        _cancellationToken = cancellationToken;
        _isInitialized = true;

        // If persistence is enabled and this narration has a StoryInstanceId,
        // we might be resuming - load any previous state
        if (_options.EnablePersistence && narration.StoryInstanceId != Auid.Empty)
        {
            await LoadStoryState(narration.StoryInstanceId);
            _logger.LogInformation("Story {StoryId} resuming from saved state", narration.StoryInstanceId);
        }
        else if (_options.EnablePersistence)
        {
            // Generate new story ID for first execution
            var storyId = Auid.New("STR");
            var baseNarration = narration as dynamic;
            baseNarration.StoryInstanceId = storyId;
            _logger.LogInformation("Story started with ID {StoryId}", storyId);
        }
    }

    /// <summary>
    /// Set user input for an interactive chapter when resuming.
    /// Must be called before executing the story if resuming an interactive chapter.
    /// </summary>
    public void SetChapterInput(JsonElement? input)
    {
        _chapterInput = input;
        _logger.LogDebug("Chapter input set for resume");
    }

    /// <summary>
    /// Execute a single chapter.
    /// Handles skipping (for resume), error collection, and persistence.
    /// </summary>
    public async Task ExecuteChapter<TChapter, TNarration>(TNarration narration)
        where TChapter : IChapter<TNarration>
        where TNarration : class
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("StoryEngine must be initialized before executing chapters");
        }

        // Skip if failed (and stop-on-error is enabled)
        // Note: Don't skip if paused but we have chapter input (resume scenario)
        if ((_isPaused && _chapterInput == null) || (_hasFailed && _options.StopOnFirstError))
        {
            _logger.LogDebug("Skipping chapter execution - story is paused (no input) or failed");
            return;
        }

        // Resolve the chapter from DI
        var chapter = _serviceProvider.GetService<TChapter>();
        if (chapter == null)
        {
            var error = new Error
            {
                Message = $"Chapter {typeof(TChapter).Name} not registered in DI container",
                Description = "Ensure RegisterStories() is called and the chapter is in the scanned assembly"
            };
            HandleChapterFailure(null, error);
            return;
        }

        // After null check, chapter is guaranteed non-null
        var chapterId = chapter.ChapterId;

        // Skip chapters until we reach the resume point
        if (_resumeFromChapterId != null && chapterId != _resumeFromChapterId)
        {
            _logger.LogDebug("Skipping chapter {ChapterId} during resume", chapterId);
            return;
        }

        // Clear resume flag once we reach the target chapter
        if (_resumeFromChapterId == chapterId)
        {
            _resumeFromChapterId = null;
            _logger.LogInformation("Resuming story at chapter {ChapterId}", chapterId);
        }

        var chapterInfo = new ChapterInfo
        {
            ChapterId = chapterId,
            StartedAt = DateTime.UtcNow,
            Status = StoryStatus.Running
        };

        _logger.LogInformation("Executing chapter: {ChapterId}", chapterId);

        try
        {
            _cancellationToken.ThrowIfCancellationRequested();

            Result result;

            // Determine if this is an interactive chapter
            var baseType = typeof(TChapter).BaseType;
            var isInteractive = baseType?.IsGenericType == true &&
                                baseType.GetGenericTypeDefinition() == typeof(InteractiveChapter<,>);

            if (isInteractive)
            {
                result = await ExecuteInteractiveChapter(chapter, narration, chapterInfo);
            }
            else
            {
                result = await chapter.Read(narration);
            }

            if (result.IsFailure)
            {
                HandleChapterFailure(chapterInfo, result.Error!);
            }
            else if (chapterInfo.Status != StoryStatus.WaitingForInput)
            {
                // Only mark as completed if not waiting for input
                chapterInfo.Status = StoryStatus.Completed;
                chapterInfo.FinishedAt = DateTime.UtcNow;
                _logger.LogInformation("Chapter {ChapterId} completed successfully", chapterId);
            }
        }
        catch (OperationCanceledException)
        {
            chapterInfo.Status = StoryStatus.Cancelled;
            chapterInfo.FinishedAt = DateTime.UtcNow;
            _logger.LogWarning("Chapter {ChapterId} was cancelled", chapterId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chapter {ChapterId} threw an exception", chapterId);
            HandleChapterFailure(chapterInfo, Error.From(ex));
        }

        // Update existing chapter in history or add new one
        var existingIndex = _chapterHistory.FindIndex(h => h.ChapterId == chapterId);
        if (existingIndex >= 0)
        {
            // Update existing entry (for resume scenarios where chapter is re-executed)
            _chapterHistory[existingIndex] = chapterInfo;
            _logger.LogDebug("Updated chapter {ChapterId} in history", chapterId);
        }
        else
        {
            // Add new entry
            _chapterHistory.Add(chapterInfo);
        }

        // Persist state if enabled
        if (_options.EnablePersistence)
        {
            await SaveStoryState();
        }
    }

    /// <summary>
    /// Execute an interactive chapter - handles schema and user input.
    /// </summary>
    private async Task<Result> ExecuteInteractiveChapter<TNarration>(
        IChapter<TNarration> chapter,
        TNarration narration,
        ChapterInfo chapterInfo)
        where TNarration : class
    {
        chapterInfo.ChapterType = "Interactive";

        // Get the input schema via reflection
        var getSchemaMethod = chapter.GetType().GetMethod("GetRequiredInputSchema");
        if (getSchemaMethod != null)
        {
            var schema = getSchemaMethod.Invoke(chapter, null) as List<DataField>;
            chapterInfo.RequiredData = schema ?? new List<DataField>();
        }

        // Check if we have user input
        if (_chapterInput == null)
        {
            // No input yet - pause the story
            _isPaused = true;
            chapterInfo.Status = StoryStatus.WaitingForInput;

            // Update narration's current chapter
            if (_narration is Narration<object, object> baseNarration)
            {
                baseNarration.CurrentChapterId = chapter.ChapterId;
            }

            _logger.LogInformation(
                "Story paused at interactive chapter {ChapterId}, waiting for user input",
                chapter.ChapterId);

            return Result.Success(); // Not a failure, just waiting
        }

        // We have input - deserialize and execute
        var chapterType = chapter.GetType();
        var baseType = chapterType.BaseType;
        var inputType = baseType!.GetGenericArguments()[1]; // TChapterInput

        var deserializedInput = JsonSerializer.Deserialize(_chapterInput.Value, inputType);
        chapterInfo.ProvidedData = _chapterInput;

        // Call ExecuteWithInput via reflection
        var executeMethod = chapterType.GetMethod("ReadWithInput");
        var task = executeMethod!.Invoke(chapter, new[] { narration, deserializedInput }) as Task<Result>;
        var result = await task!;

        // Clear pause flag and chapter input after successful execution
        if (result.IsSuccess)
        {
            _isPaused = false;
            _chapterInput = null;
        }

        chapterInfo.FinishedAt = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// Handle a chapter failure - record error and update state.
    /// </summary>
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

        _logger.LogError("Chapter failed: {ErrorMessage}", error.Message);
    }

    /// <summary>
    /// Get the final result of story execution.
    /// Returns the output if successful, or an error if failed/paused.
    /// </summary>
    public Result<TOutput> GetResult<TOutput>() where TOutput : class, new()
    {
        // Check for errors first (including validation failures from interactive chapters)
        // This ensures that validation errors take precedence over pause status
        if (_errors.Any())
        {
            if (_errors.Count == 1)
            {
                return Result<TOutput>.Fail(_errors[0]);
            }

            return Result<TOutput>.Fail(new AggregateError(_errors));
        }

        // Then check for pause (only if no errors occurred)
        if (_isPaused)
        {
            var narrationBase = _narration as Narration<object, object>;
            return Result<TOutput>.Fail(new Error
            {
                Message = "Story paused waiting for user input",
                Description = $"Current chapter: {narrationBase?.CurrentChapterId}"
            });
        }

        // Extract output from narration
        var narrationProperty = _narration.GetType().GetProperty("Output");
        var output = narrationProperty?.GetValue(_narration);

        if (output is TOutput typedOutput)
        {
            return Result<TOutput>.Success(typedOutput);
        }

        return Result<TOutput>.Fail("Failed to extract output from narration");
    }

    /// <summary>
    /// Save current story state to the repository.
    /// </summary>
    private async Task SaveStoryState()
    {
        if (_options.Repository == null)
        {
            _logger.LogWarning("Persistence enabled but no repository configured");
            return;
        }

        var narrationBase = _narration as dynamic;
        Auid storyId = narrationBase?.StoryInstanceId ?? Auid.Empty;

        if (storyId == Auid.Empty)
        {
            _logger.LogWarning("Cannot save story state - no StoryInstanceId");
            return;
        }

        var storyInstance = new StoryInstance
        {
            StoryId = storyId,
            HandlerTypeName = _handlerTypeName,
            Status = _isPaused ? StoryStatus.WaitingForInput :
                     _hasFailed ? StoryStatus.Failed :
                     StoryStatus.Running,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = new List<ChapterInfo>(_chapterHistory),
            CurrentChapter = _isPaused ? _chapterHistory.LastOrDefault() : null,
            Context = JsonSerializer.Serialize(_narration, StoryJsonOptions.Default)
        };

        await _options.Repository.SaveAsync(storyInstance);
        _logger.LogDebug("Story state saved for {StoryId}", storyId);
    }

    /// <summary>
    /// Load story state from the repository and restore engine state.
    /// </summary>
    private async Task LoadStoryState(Auid storyId)
    {
        if (_options.Repository == null)
        {
            _logger.LogWarning("Persistence enabled but no repository configured");
            return;
        }

        var storyInstance = await _options.Repository.FindById(storyId);
        if (storyInstance == null)
        {
            _logger.LogWarning("Story {StoryId} not found in repository", storyId);
            return;
        }

        // Restore chapter history
        _chapterHistory.Clear();
        _chapterHistory.AddRange(storyInstance.History);

        // Restore state flags
        _isPaused = storyInstance.Status == StoryStatus.WaitingForInput;
        _hasFailed = storyInstance.Status == StoryStatus.Failed;

        // If we're paused at an interactive chapter, set up for resume
        if (_isPaused && storyInstance.CurrentChapter != null)
        {
            _resumeFromChapterId = storyInstance.CurrentChapter.ChapterId;
            _logger.LogInformation(
                "Story will resume at interactive chapter {ChapterId}",
                _resumeFromChapterId);
        }

        // Note: Narration context restoration happens in StoryManager/StoryHandler
        // since the engine doesn't know the concrete narration type
        _logger.LogDebug("Story state loaded for {StoryId}", storyId);
    }
}
