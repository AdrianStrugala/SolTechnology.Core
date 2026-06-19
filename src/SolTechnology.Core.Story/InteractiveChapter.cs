using SolTechnology.Core;
using SolTechnology.Core.Story.Models;

namespace SolTechnology.Core.Story;

/// <summary>
/// Base class for interactive story chapters that require user input.
/// The story pauses at these chapters and waits for external input before continuing.
/// </summary>
/// <typeparam name="TContext">The Context type that flows through this chapter.</typeparam>
/// <typeparam name="TChapterInput">The input type that the user must provide.</typeparam>
public abstract class InteractiveChapter<TContext, TChapterInput>
    : Chapter<TContext>, IInteractiveChapter<TContext, TChapterInput>
    where TContext : class
    where TChapterInput : class, new()
{
    /// <summary>
    /// Returns the schema describing the expected input for this chapter.
    /// Uses reflection over <typeparamref name="TChapterInput"/>.
    /// </summary>
    public virtual IReadOnlyList<DataField> GetRequiredInputSchema() =>
        typeof(TChapterInput).ToDataFields();

    /// <summary>
    /// Process user-provided input for this chapter.
    /// Validate the input, update the Context based on user choices, and return success or failure.
    /// </summary>
    public abstract Task<Result> ReadWithInput(TContext context, TChapterInput userInput);

    /// <summary>
    /// Interactive chapters are orchestrated by <c>StoryEngine</c> via <see cref="ReadWithInput"/>.
    /// Calling this directly is a programming error.
    /// </summary>
    public sealed override Task<Result> Read(TContext context) =>
        throw new InvalidOperationException(
            $"Interactive chapter '{ChapterId}' requires user input. Use ReadWithInput() or orchestrate through StoryManager/StoryHandler.");
}
