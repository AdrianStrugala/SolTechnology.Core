namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Records a bounded tree of named steps for the current root operation (an HTTP request, a job,
/// a message, or a manual <see cref="Begin"/>) and emits one report when the root completes.
/// The tree follows the async flow, so parallel branches build isolated subtrees.
/// </summary>
/// <example>
/// <code>
/// using var step = chronicle.Step("ImportCities");
/// step.Set("Count", cities.Count);
/// </code>
/// </example>
public interface IChronicle
{
    /// <summary>
    /// Starts a root when no root is open in the current async flow; otherwise starts a child of
    /// the current step, so nested operations stay in one report. Dispose the handle to close it.
    /// </summary>
    /// <param name="name">Step name shown in the report.</param>
    ChronicleStep Begin(string name);

    /// <summary>
    /// Starts a child of the current step. Outside an open root, or when Chronicle is disabled,
    /// returns a non-recording handle and records nothing.
    /// </summary>
    /// <param name="name">Step name shown in the report.</param>
    ChronicleStep Step(string name);

    /// <summary>
    /// A non-owning handle to the current step: <c>Set</c>, <c>Fail</c> and <c>Cancel</c> apply
    /// to it, and <c>Dispose</c> does nothing, so it can never close an ancestor.
    /// </summary>
    ChronicleStep Current { get; }

    /// <summary><c>true</c> when Chronicle is enabled and a root is open in the current async flow.</summary>
    bool IsActive { get; }
}
