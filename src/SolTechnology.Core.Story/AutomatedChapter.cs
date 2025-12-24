using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.Story;

/// <summary>
/// Base class for automated story chapters that execute without user input.
/// These chapters run to completion automatically during story execution.
/// Use this for business logic that doesn't require external interaction.
/// </summary>
/// <typeparam name="TNarration">The narration type that flows through this chapter</typeparam>
/// <example>
/// <code>
/// public class CalculateTotal : AutomatedChapter&lt;OrderNarration&gt;
/// {
///     public override Task&lt;Result&gt; Read(OrderNarration narration)
///     {
///         narration.Total = narration.Items.Sum(i => i.Price * i.Quantity);
///         return Result.SuccessAsTask();
///     }
/// }
/// </code>
/// </example>
public abstract class AutomatedChapter<TNarration> : IChapter<TNarration>
    where TNarration : class
{
    /// <summary>
    /// Unique identifier for this chapter.
    /// Defaults to the type name. Override if you need custom identification.
    /// </summary>
    public virtual string ChapterId => GetType().Name;

    /// <summary>
    /// Execute this chapter's automated logic.
    /// Read from narration, perform your business logic, then update narration with results.
    /// Return Result.Success() if successful, or Result.Fail("message") if something goes wrong.
    /// </summary>
    /// <param name="narration">The narration containing all story data</param>
    /// <returns>Result indicating success or failure</returns>
    public abstract Task<Result> Read(TNarration narration);
}
