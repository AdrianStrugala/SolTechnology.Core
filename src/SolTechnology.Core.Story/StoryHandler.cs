using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story;

/// <summary>
/// Base handler for orchestrating multi-chapter stories.
/// Supports both simple automated chains and complex pausable flows with persistence.
/// Use TellStory() to define your story's chapter sequence.
/// </summary>
/// <typeparam name="TInput">The input type that initiates the story</typeparam>
/// <typeparam name="TNarration">The narration type that flows through chapters</typeparam>
/// <typeparam name="TOutput">The output type returned when the story completes</typeparam>
/// <example>
/// <code>
/// public class SaveCityStory : StoryHandler&lt;SaveCityInput, SaveCityNarration, SaveCityResult&gt;
/// {
///     public SaveCityStory(IServiceProvider sp, ILogger&lt;SaveCityStory&gt; logger)
///         : base(sp, logger) { }
///
///     protected override async Task TellStory()
///     {
///         await ReadChapter&lt;LoadExistingCity&gt;();
///         await ReadChapter&lt;AssignAlternativeName&gt;();
///         await ReadChapter&lt;IncrementSearchCount&gt;();
///         await ReadChapter&lt;SaveToDatabase&gt;();
///     }
/// }
/// </code>
/// </example>
public abstract class StoryHandler<TInput, TNarration, TOutput>
    where TInput : class
    where TNarration : Narration<TInput, TOutput>, new()
    where TOutput : class, new()
{
    /// <summary>
    /// The narration (context) flowing through this story.
    /// Access this in your TellStory() method if you need to set output directly.
    /// </summary>
    public TNarration Narration { get; set; } = null!;

    private readonly StoryEngine _engine;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor for story handlers.
    /// Repository and StopOnFirstError are injected from DI based on RegisterStories() configuration.
    /// </summary>
    /// <param name="serviceProvider">DI container for resolving chapters</param>
    /// <param name="logger">Logger for this story handler</param>
    protected StoryHandler(
        IServiceProvider serviceProvider,
        ILogger<StoryHandler<TInput, TNarration, TOutput>> logger)
    {
        _logger = logger;

        // Try to resolve IStoryRepository from DI (optional - null if not registered)
        var repository = serviceProvider.GetService<IStoryRepository>();

        // Get StopOnFirstError from StoryOptions if available, otherwise default to true
        var options = serviceProvider.GetService<StoryOptions>();
        var stopOnFirstError = options?.StopOnFirstError ?? true;

        _engine = new StoryEngine(serviceProvider, logger, repository, stopOnFirstError);
    }

    /// <summary>
    /// Define the sequence of chapters in your story.
    /// Call ReadChapter&lt;TChapter&gt;() for each chapter in order.
    /// This method reads like a table of contents for your story.
    /// </summary>
    /// <example>
    /// <code>
    /// protected override async Task TellStory()
    /// {
    ///     await ReadChapter&lt;ValidateInput&gt;();
    ///     await ReadChapter&lt;ProcessData&gt;();
    ///     await ReadChapter&lt;SaveResults&gt;();
    /// }
    /// </code>
    /// </example>
    protected abstract Task TellStory();

    /// <summary>
    /// Execute a chapter in your story.
    /// The chapter is resolved from DI, executed, and its result is tracked.
    /// For simple workflows: executes immediately.
    /// For pausable workflows: resumes from the correct chapter based on saved state.
    /// </summary>
    /// <typeparam name="TChapter">The chapter type to execute</typeparam>
    protected async Task ReadChapter<TChapter>() where TChapter : IChapter<TNarration>
    {
        await _engine.ExecuteChapter<TChapter, TNarration>(Narration);
    }

    /// <summary>
    /// Public entry point for executing the story.
    /// For simple workflows: executes all chapters sequentially and returns the result.
    /// For pausable workflows: can be called multiple times to resume from saved state.
    /// </summary>
    /// <param name="input">The input that initiates this story</param>
    /// <param name="cancellationToken">Cancellation token to stop execution</param>
    /// <returns>
    /// Result&lt;TOutput&gt; containing:
    /// - Success with output if all chapters completed successfully
    /// - Failure with error if any chapter failed
    /// - Failure with "paused" status if waiting for interactive input
    /// </returns>
    public virtual async Task<Result<TOutput>> Handle(
        TInput input,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting story {StoryName} with input {InputType}",
            GetType().Name,
            typeof(TInput).Name);

        try
        {
            // Create narration if not already set (for resume scenarios)
            if (Narration == null || Narration.StoryInstanceId == Auid.Empty)
            {
                Narration = new TNarration { Input = input };
            }

            // Initialize the engine
            await _engine.Initialize(Narration, GetType().Name, cancellationToken);

            // Execute the story (calls TellStory() which calls Chapter<T>() methods)
            await TellStory();

            // Get the final result
            var result = _engine.GetResult<TOutput>();

            if (result.IsSuccess)
            {
                _logger.LogInformation("Story {StoryName} completed successfully", GetType().Name);
            }
            else
            {
                _logger.LogWarning(
                    "Story {StoryName} failed: {ErrorMessage}",
                    GetType().Name,
                    result.Error?.Message);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Story {StoryName} was cancelled", GetType().Name);
            return Result<TOutput>.Fail("Story execution was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Story {StoryName} threw an unhandled exception", GetType().Name);
            return Result<TOutput>.Fail(Error.From(ex));
        }
    }
}
