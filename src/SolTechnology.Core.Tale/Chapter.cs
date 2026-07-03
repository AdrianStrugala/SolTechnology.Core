namespace SolTechnology.Core.Tale;

/// <summary>
/// Base class for tale chapters.
/// Chapters are the building blocks of your tale - each represents a focused step in your business logic.
/// For chapters that need user input, use InteractiveChapter instead.
/// </summary>
/// <typeparam name="TContext">The Context type that flows through this chapter</typeparam>
/// <example>
/// <code>
/// public class CalculateTotal : Chapter&lt;OrderContext&gt;
/// {
///     public override Task&lt;Result&gt; Read(OrderContext context)
///     {
///         context.Total = context.Items.Sum(i => i.Price * i.Quantity);
///         return Result.SuccessAsTask();
///     }
/// }
/// </code>
/// </example>
public abstract class Chapter<TContext> : IChapter<TContext>
    where TContext : class
{
    /// <summary>
    /// Unique identifier for this chapter.
    /// Defaults to the type name. Override if you need custom identification.
    /// </summary>
    public virtual string ChapterId => GetType().Name;

    /// <summary>
    /// Execute this chapter's logic.
    /// Read from context, perform your business logic, then update Context with results.
    /// Return Result.Success() if successful, or Result.Fail("message") if something goes wrong.
    /// </summary>
    /// <param name="context">The Context containing all tale data</param>
    /// <returns>Result indicating success or failure</returns>
    public abstract Task<Result> Read(TContext context);
}
