using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Story.Orchestration;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story;

/// <summary>
/// Base class for story handlers that orchestrate chapter execution.
/// Supports both automated workflows and interactive workflows with pause/resume.
/// </summary>
/// <typeparam name="TInput">The input type for the story</typeparam>
/// <typeparam name="TContext">The context type carrying state through chapters</typeparam>
/// <typeparam name="TOutput">The output type produced by the story</typeparam>
/// <example>
/// <code>
/// public class SaveCityStory : StoryHandler&lt;SaveCityInput, SaveCitycontext, SaveCityResult&gt;
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
public abstract class StoryHandler<TInput, TContext, TOutput>
    where TInput : class
    where TContext : Context<TInput, TOutput>, new()
    where TOutput : class, new()
{
    /// <summary>
    /// Gets or sets the story context. Context carries state across chapters.
    /// </summary>
    public TContext Context { get; set; } = null!;

    private readonly StoryEngine<TContext> _engine;
    private readonly ILogger _logger;

    protected StoryHandler(
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _logger = logger;
        
        var repository = serviceProvider.GetService<IStoryRepository>();
        var options = serviceProvider.GetService<StoryOptions>();
        
        _engine = new StoryEngine<TContext>(
            serviceProvider, 
            logger, 
            repository, 
            options?.StopOnFirstError ?? true);
    }

    /// <summary>
    /// Defines the story flow by calling ReadChapter for each chapter in sequence.
    /// Override this method to define your story's narrative.
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
    /// Executes a chapter in the story.
    /// </summary>
    /// <typeparam name="TChapter">The chapter type to execute</typeparam>
    protected async Task ReadChapter<TChapter>() where TChapter : IChapter<TContext>
    {
        await _engine.ExecuteChapter<TChapter>(Context);
    }

    /// <summary>
    /// Handles story execution when used as a MediatR query/command.
    /// Routes to the full Handle method with no resume input.
    /// </summary>
    /// <param name="input">The input for the story</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with output or error</returns>
    public Task<Result<TOutput>> Handle(TInput input, CancellationToken cancellationToken)
    {
        return Handle(input, null, cancellationToken);
    }

    /// <summary>
    /// Executes the story with optional resume input for interactive chapters.
    /// Used by StoryManager for managing interactive workflows.
    /// </summary>
    /// <param name="input">The input for the story</param>
    /// <param name="resumeInput">Optional user input for resuming an interactive chapter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with output or error</returns>
    public virtual async Task<Result<TOutput>> Handle(
        TInput input,
        JsonElement? resumeInput = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Story {Handler} processing...", GetType().Name);

        try
        {
            // Initialize Context if starting fresh
            if (Context == null || Context.StoryInstanceId == Auid.Empty)
            {
                Context = new TContext { Input = input };
            }

            // Initialize engine with Context and potential Resume Input
            await _engine.Initialize(Context, GetType().Name, resumeInput, cancellationToken);

            // Execute chapters
            await TellStory();

            return _engine.GetResult<TOutput>();
        }
        catch (OperationCanceledException)
        {
            return Result<TOutput>.Fail("Story execution cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Story {Handler} failed", GetType().Name);
            return Result<TOutput>.Fail(Error.From(ex));
        }
    }
}