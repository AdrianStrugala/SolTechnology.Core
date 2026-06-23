namespace SolTechnology.Core.Story;

/// <summary>
/// Engine-level policies for the Story framework. Does not own persistence — the repository
/// is chosen and registered independently through the <see cref="Builder.IStoryBuilder"/>
/// returned by <c>RegisterStories(...)</c> (e.g. <c>UseInMemoryStoryRepository</c>,
/// <c>UseStoryRepository&lt;T&gt;</c>). A repository is
/// always present — in-memory is the default.
/// </summary>
public sealed class StoryOptions
{

    /// <summary>
    /// Prefix used when generating new <see cref="Auid"/> story identifiers. Use distinct
    /// prefixes to separate tenants or environments in a shared persistence store.
    /// </summary>
    public string StoryIdPrefix { get; set; } = "STR";


    /// <summary>
    /// When true, <see cref="Api.StoryController"/> exposes only handlers that were
    /// registered via <c>RegisterStories</c>. Defaults to true — strongly recommended.
    /// </summary>
    public bool RestrictControllerToRegisteredHandlers { get; set; } = true;

    /// <summary>
    /// Internal fallback used by the engine when options are not present in DI (e.g. when a
    /// <see cref="StoryHandler{TInput,TContext,TOutput}"/> is resolved from a container that
    /// does not call <c>RegisterStories</c>). Application code should not depend on this.
    /// </summary>
    internal static StoryOptions Default { get; } = new();
}
