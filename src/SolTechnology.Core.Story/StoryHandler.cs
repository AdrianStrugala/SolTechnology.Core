using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Story.Orchestration;
using SolTechnology.Core.Story.Persistence;
using SolTechnology.Core.Story.Tale;

namespace SolTechnology.Core.Story;

/// <summary>
/// Base handler for orchestrating multi-chapter stories.
/// Supports both simple automated workflows and interactive (pausable) workflows with persistence.
/// Override <see cref="Tell"/> to describe the chapter sequence as a <see cref="Tale{TOutput}"/> —
/// it reads like a table of contents.
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
///     protected override Tale&lt;SaveCityResult&gt; Tell() =&gt;
///         Open&lt;LoadExistingCity&gt;()
///             .Read&lt;AssignAlternativeName&gt;()
///             .Read&lt;IncrementSearchCount&gt;()
///             .Read&lt;SaveToDatabase&gt;()
///             .Finale(ctx =&gt; ctx.Output);
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
///     protected override Tale&lt;OnboardingOutput&gt; Tell() =&gt;
///         Open&lt;CollectBasicInfoChapter&gt;()   // pauses for user input
///             .Read&lt;VerifyEmailChapter&gt;()     // pauses for email verification
///             .Read&lt;SetupPreferencesChapter&gt;() // pauses for preferences
///             .Read&lt;CompleteOnboardingChapter&gt;() // automated finalization
///             .Finale(ctx =&gt; ctx.Output);
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
    /// Defines the story flow. Override this to narrate the chapter sequence as a fluent
    /// <see cref="Tale{TOutput}"/> — it should read like a table of contents for the workflow.
    /// Start with <see cref="Open{TChapter}"/>, chain chapters with <c>Read</c>, guard with
    /// <c>Expect</c>, recover with <c>Otherwise</c>, and conclude with <c>Finale</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// protected override Tale&lt;OrderResult&gt; Tell() =&gt;
    ///     Open&lt;ValidateInput&gt;()
    ///         .Read&lt;ProcessData&gt;()
    ///         .Read&lt;SaveResults&gt;()
    ///         .Finale(ctx =&gt; ctx.Output);
    /// </code>
    /// </example>
    /// <remarks>
    /// <c>Tell()</c> must be deterministic. It is re-invoked on every <c>Handle</c> call — including
    /// each resume of a paused story — and the engine replays the rebuilt plan against the persisted
    /// chapter history. Branch on context state via <c>Expect</c> / <c>Otherwise</c>, never on ambient
    /// inputs (clock, random, feature flags) that can differ between the original run and a resume,
    /// or the story will resume on the wrong step.
    /// </remarks>
    protected abstract Tale<TOutput> Tell();

    /// <summary>
    /// Opens a story with its first chapter, returning a <see cref="Tale{TContext,TOutput}"/> to
    /// continue building. For automated chapters the engine runs them immediately; for
    /// <see cref="InteractiveChapter{TContext,TInput}"/> it pauses on the first run and resumes from
    /// persisted state on a subsequent <c>StoryManager.ResumeStory</c> call.
    /// </summary>
    /// <typeparam name="TChapter">The first chapter to read; resolved from DI.</typeparam>
    /// <example>
    /// <code>
    /// Open&lt;LoadCustomer&gt;()
    ///     .Read&lt;CollectBasicInfoChapter&gt;() // interactive — pauses here
    ///     .Read&lt;FinalizeOrder&gt;()
    ///     .Finale(ctx =&gt; ctx.Output);
    /// </code>
    /// </example>
    protected Tale<TContext, TOutput> Open<TChapter>() where TChapter : IChapter<TContext>
        => Tale<TContext, TOutput>.Open<TChapter>();

    /// <summary>Opens a story with an inline step, for flows whose first action is not a dedicated chapter.</summary>
    protected Tale<TContext, TOutput> Open(Action<TContext> step)
        => Tale<TContext, TOutput>.Open(step);

    /// <summary>Opens a story with an inline step that may itself fail.</summary>
    protected Tale<TContext, TOutput> Open(Func<TContext, Task<Result>> step)
        => Tale<TContext, TOutput>.Open(step);

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

            var tale = Tell();
            await _engine.Run(tale.Steps);

            var result = _engine.GetResult(tale.Conclusion);
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
