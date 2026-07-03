namespace SolTechnology.Core.Tale;

/// <summary>
/// Marker interface for chapters that require external user input to make progress.
/// Inherits from <see cref="IChapter{TContext}"/> for compatibility with the orchestration
/// pipeline, but consumers should prefer <see cref="ReadWithInput"/> over the inherited
/// <see cref="IChapter{TContext}.Read"/> (which always throws for interactive chapters).
/// </summary>
/// <typeparam name="TContext">Context type flowing through the tale.</typeparam>
/// <typeparam name="TChapterInput">Input required from the caller.</typeparam>
public interface IInteractiveChapter<in TContext, in TChapterInput> : IChapter<TContext>
    where TContext : class
{
    /// <summary>
    /// Describes the shape of <typeparamref name="TChapterInput"/> for API consumers.
    /// </summary>
    IReadOnlyList<Models.DataField> GetRequiredInputSchema();

    /// <summary>
    /// Processes user input and advances the tale.
    /// </summary>
    Task<Result> ReadWithInput(TContext context, TChapterInput userInput);
}

