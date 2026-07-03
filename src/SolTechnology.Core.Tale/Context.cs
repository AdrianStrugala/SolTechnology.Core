namespace SolTechnology.Core.Tale;

/// <summary>
/// Base context for tale execution.
/// Holds input, output, and intermediate data that flows through all chapters of the tale.
/// Think of context as the thread connecting all chapters - it carries the tale's state forward.
/// </summary>
/// <typeparam name="TInput">The input type that initiates the tale</typeparam>
/// <typeparam name="TOutput">The output type returned when the tale completes</typeparam>
public abstract class Context<TInput, TOutput>
    where TInput : class
    where TOutput : class, new()
{
    /// <summary>
    /// The input that started this tale.
    /// Set automatically by the TaleHandler - you don't need to populate this.
    /// </summary>
    public TInput Input { get; set; } = default!;

    /// <summary>
    /// The output that will be returned when the tale completes.
    /// Initialize intermediate data in your chapters, then set the final output in the last chapter.
    /// </summary>
    public TOutput Output { get; set; } = new();

    /// <summary>
    /// Internal identifier for the tale instance.
    /// Used by the framework for persistence - you don't need to access this.
    /// </summary>
    public Auid TaleInstanceId { get; internal set; } = Auid.Empty;

    /// <summary>
    /// Internal identifier for the current chapter being executed.
    /// Used by the framework for pause/resume - you don't need to access this.
    /// </summary>
    public string? CurrentChapterId { get; internal set; }
}
