using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Story.Orchestration;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story;

/// <summary>
/// Base handler for orchestrating multi-chapter stories.
/// Supports both simple automated workflows and interactive (pausable) workflows with persistence.
/// Override <see cref="TellStory"/> to describe the chapter sequence — it reads like a table of contents.
/// </summary>
/// <typeparam name="TInput">The input type that initiates the story.</typeparam>
/// <typeparam name="TContext">The context type carrying state through chapters.</typeparam>
/// <typeparam name="TOutput">The output type returned when the story completes.</typeparam>
/// <example>
/// Automated story (no user interaction):
/// <code>
/// public class SaveCityStory : StoryHandler&lt;SaveCityInput, SaveCityContext, SaveCityResult&gt;
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
/// <example>
/// Interactive story (pauses for user input, requires persistence):
/// <code>
/// public class UserOnboardingStory : StoryHandler&lt;OnboardingInput, OnboardingContext, OnboardingOutput&gt;
/// {
///     public UserOnboardingStory(IServiceProvider sp, ILogger&lt;UserOnboardingStory&gt; logger)
///         : base(sp, logger) { }
///
///     protected override async Task TellStory()
///     {
///         await ReadChapter&lt;CollectBasicInfoChapter&gt;();   // pauses for user input
///         await ReadChapter&lt;VerifyEmailChapter&gt;();        // pauses for email verification
///         await ReadChapter&lt;SetupPreferencesChapter&gt;();   // pauses for preferences
///         await ReadChapter&lt;CompleteOnboardingChapter&gt;(); // automated finalization
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
    /// The story context. Settable only by the framework; external mutation during
    /// a single <c>Handle</c> invocation is not supported.
    /// </summary>
    public TContext Context { get; internal set; } = null!;

    private StoryEngine<TInput, TContext, TOutput> _engine = null!;
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    protected StoryHandler(IServiceProvider serviceProvider, ILogger logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Defines the story flow. Override this method to narrate the chapter sequence —
    /// it should read like a table of contents for the workflow.
    /// </summary>
    /// <example>
    /// <code>
    /// protected override async Task TellStory()
    /// {
    ///     await ReadChapter&lt;ValidateInput&gt;();
    ///     await ReadChapter&lt;ProcessData&gt;();
    ///     await ReadChapter&lt;SaveResults&gt;();
    ///
    ///     Context.Output.OrderId = Context.ProcessedOrderId;
    /// }
    /// </code>
    /// </example>
    protected abstract Task TellStory();

    /// <summary>
    /// Executes a single chapter resolved from DI.
    /// For automated chapters: runs immediately.
    /// For <see cref="InteractiveChapter{TContext,TInput}"/>: either pauses (first run) or
    /// resumes from persisted state on a subsequent <c>StoryManager.ResumeStory</c> call.
    /// </summary>
    /// <typeparam name="TChapter">The chapter type to execute.</typeparam>
    /// <example>
    /// <code>
    /// await ReadChapter&lt;LoadCustomer&gt;();
    /// await ReadChapter&lt;CollectBasicInfoChapter&gt;(); // interactive — pauses here
    /// await ReadChapter&lt;FinalizeOrder&gt;();
    /// </code>
    /// </example>
    protected Task ReadChapter<TChapter>() where TChapter : IChapter<TContext>
        => _engine.ExecuteChapter<TChapter>();

    /// <summary>
    /// Entry point compatible with CQRS <c>IQueryHandler</c>/<c>ICommandHandler</c>.
    /// </summary>
    public Task<Result<TOutput>> Handle(TInput input, CancellationToken cancellationToken)
        => Handle(input, null, cancellationToken);

    /// <summary>
    /// Executes the story with optional resume input.
    /// </summary>
    public virtual async Task<Result<TOutput>> Handle(
        TInput input,
        JsonElement? resumeInput = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Story {Handler} processing...", GetType().Name);

        var repository = _serviceProvider.GetService<IStoryRepository>();
        var options = _serviceProvider.GetService<StoryOptions>() ?? StoryOptions.Default;
        _engine = new StoryEngine<TInput, TContext, TOutput>(_serviceProvider, _logger, repository, options);

        if (Context is null || Context.StoryInstanceId == Auid.Empty)
        {
            Context = new TContext { Input = input };
        }

        using var _ = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["StoryHandler"] = GetType().Name,
            ["StoryId"] = Context.StoryInstanceId
        });

        try
        {
            var init = await _engine.Initialize(Context, GetType(), resumeInput, cancellationToken);
            if (init.IsFailure)
            {
                await SafePersistTerminalState();
                return Result<TOutput>.Fail(init.Error!);
            }

            await TellStory();

            var result = _engine.GetResult();
            await SafePersistTerminalState();
            return result;
        }
        catch (OperationCanceledException)
        {
            await SafePersistTerminalState();
            return Result<TOutput>.Fail(new StoryCancelledError(Context.StoryInstanceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Story {Handler} failed", GetType().Name);
            await SafePersistTerminalState();
            return Result<TOutput>.Fail(Error.From(ex));
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
            _logger.LogWarning(ex, "Failed to persist terminal story state for {StoryId}", Context.StoryInstanceId);
        }
    }
}
