namespace SolTechnology.Core.Tale.Models;

/// <summary>
/// Represents the current status of a tale or chapter.
/// </summary>
public enum TaleStatus
{
    /// <summary>
    /// Tale has been created but not yet started.
    /// </summary>
    Created,

    /// <summary>
    /// Tale is currently executing chapters.
    /// </summary>
    Running,

    /// <summary>
    /// Tale is paused and waiting for user input at an interactive chapter.
    /// </summary>
    WaitingForInput,

    /// <summary>
    /// Tale has completed all chapters successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Tale failed due to one or more chapter errors.
    /// </summary>
    Failed,

    /// <summary>
    /// Tale was cancelled by the user or system.
    /// </summary>
    Cancelled
}
