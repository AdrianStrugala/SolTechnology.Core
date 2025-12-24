using System.Text.Json;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story.Models;

namespace SolTechnology.Core.Story;

/// <summary>
/// Base class for interactive story chapters that require user input.
/// The story pauses at these chapters and waits for external input before continuing.
/// Use this for steps that need approval, additional data, or human decision-making.
/// </summary>
/// <typeparam name="TNarration">The narration type that flows through this chapter</typeparam>
/// <typeparam name="TChapterInput">The input type that the user must provide</typeparam>
/// <example>
/// <code>
/// public class RequestApproval : InteractiveChapter&lt;OrderNarration, ApprovalInput&gt;
/// {
///     public override Task&lt;Result&gt; ReadWithInput(OrderNarration narration, ApprovalInput userInput)
///     {
///         if (!userInput.IsApproved)
///             return Result.FailAsTask("Order was not approved");
///
///         narration.ApprovalTimestamp = DateTime.UtcNow;
///         narration.ApprovedBy = userInput.ApproverName;
///         return Result.SuccessAsTask();
///     }
/// }
/// </code>
/// </example>
public abstract class InteractiveChapter<TNarration, TChapterInput> : IChapter<TNarration>
    where TNarration : class
    where TChapterInput : class, new()
{
    /// <summary>
    /// Unique identifier for this chapter.
    /// Defaults to the type name. Override if you need custom identification.
    /// </summary>
    public virtual string ChapterId => GetType().Name;

    /// <summary>
    /// Returns the schema describing the expected input for this chapter.
    /// Used to generate API responses and validate user input structure.
    /// Automatically introspects TChapterInput using reflection.
    /// </summary>
    /// <returns>List of fields describing the required input structure</returns>
    public List<DataField> GetRequiredInputSchema() =>
        typeof(TChapterInput).ToDataFields();

    /// <summary>
    /// Process user-provided input for this chapter.
    /// Validate the input, update the narration based on user choices, and return success or failure.
    /// </summary>
    /// <param name="narration">The narration containing all story data</param>
    /// <param name="userInput">The input provided by the user</param>
    /// <returns>Result indicating whether the input was valid and processed successfully</returns>
    public abstract Task<Result> ReadWithInput(TNarration narration, TChapterInput userInput);

    /// <summary>
    /// Internal implementation of IChapter.Read - should not be called directly.
    /// Interactive chapters must be executed via ReadWithInput() or through StoryManager orchestration.
    /// </summary>
    Task<Result> IChapter<TNarration>.Read(TNarration narration)
    {
        throw new InvalidOperationException(
            $"Interactive chapter '{ChapterId}' requires user input. " +
            "Use ReadWithInput() method or execute through StoryManager for proper orchestration.");
    }
}
