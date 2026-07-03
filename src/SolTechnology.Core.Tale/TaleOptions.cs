namespace SolTechnology.Core.Tale;

/// <summary>
/// Engine-level policies for the Tale framework. Does not own persistence — the repository
/// is chosen and registered independently through the <see cref="Builder.ITaleBuilder"/>
/// returned by <c>AddSolTale(...)</c> (e.g. <c>UseInMemoryTaleRepository</c>,
/// <c>UseTaleRepository&lt;T&gt;</c>). A repository is
/// always present — in-memory is the default.
/// </summary>
public sealed class TaleOptions
{

    /// <summary>
    /// Prefix used when generating new <see cref="Auid"/> tale identifiers. Use distinct
    /// prefixes to separate tenants or environments in a shared persistence store.
    /// </summary>
    public string TaleIdPrefix { get; set; } = "STR";


    /// <summary>
    /// When true, <see cref="Api.TaleController"/> exposes only handlers that were
    /// registered via <c>AddSolTale</c>. Defaults to true — strongly recommended.
    /// </summary>
    public bool RestrictControllerToRegisteredHandlers { get; set; } = true;

    /// <summary>
    /// Internal fallback used by the engine when options are not present in DI (e.g. when a
    /// <see cref="TaleHandler{TInput,TContext,TOutput}"/> is resolved from a container that
    /// does not call <c>AddSolTale</c>). Application code should not depend on this.
    /// </summary>
    internal static TaleOptions Default { get; } = new();
}
