using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.Story;

/// <summary>
/// Base interface for all story chapters.
/// A chapter represents a single step in your story - a focused piece of business logic.
/// Each chapter reads from and writes to the shared narration.
/// </summary>
/// <typeparam name="TNarration">The narration type that flows through this chapter</typeparam>
public interface IChapter<in TNarration> where TNarration : class
{
    /// <summary>
    /// Unique identifier for this chapter.
    /// Defaults to the type name, but you can override for custom identification.
    /// Used by the framework for logging, debugging, and pause/resume functionality.
    /// </summary>
    string ChapterId => GetType().Name;

    /// <summary>
    /// Execute this chapter's logic.
    /// Read from narration, perform your business logic, then update narration with results.
    /// Return Result.Success() if the chapter completes successfully.
    /// Return Result.Fail("message") if something goes wrong.
    /// </summary>
    /// <param name="narration">The narration containing input, output, and intermediate data</param>
    /// <returns>Result indicating success or failure of this chapter</returns>
    Task<Result> Read(TNarration narration);
}
