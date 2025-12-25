namespace SolTechnology.Core.Story;

/// <summary>
/// Base narration (context) for story execution.
/// Holds input, output, and intermediate data that flows through all chapters of the story.
/// Think of narration as the thread connecting all chapters - it carries the story's state forward.
/// </summary>
/// <typeparam name="TInput">The input type that initiates the story</typeparam>
/// <typeparam name="TOutput">The output type returned when the story completes</typeparam>
public abstract class Narration<TInput, TOutput>
    where TInput : class
    where TOutput : class, new()
{
    /// <summary>
    /// The input that started this story.
    /// Set automatically by the StoryHandler - you don't need to populate this.
    /// </summary>
    public TInput Input { get; set; } = default!;

    /// <summary>
    /// The output that will be returned when the story completes.
    /// Initialize intermediate data in your chapters, then set the final output in the last chapter.
    /// </summary>
    public TOutput Output { get; set; } = new();

    /// <summary>
    /// Internal identifier for the story instance.
    /// Used by the framework for persistence - you don't need to access this.
    /// </summary>
    public string? StoryInstanceId { get; internal set; }

    /// <summary>
    /// Internal identifier for the current chapter being executed.
    /// Used by the framework for pause/resume - you don't need to access this.
    /// </summary>
    public string? CurrentChapterId { get; internal set; }
}
