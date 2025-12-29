namespace SolTechnology.Core.Story.Models;

/// <summary>
/// Represents the current status of a story or chapter.
/// </summary>
public enum StoryStatus
{
    /// <summary>
    /// Story has been created but not yet started.
    /// </summary>
    Created,

    /// <summary>
    /// Story is currently executing chapters.
    /// </summary>
    Running,

    /// <summary>
    /// Story is paused and waiting for user input at an interactive chapter.
    /// </summary>
    WaitingForInput,

    /// <summary>
    /// Story has completed all chapters successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Story failed due to one or more chapter errors.
    /// </summary>
    Failed,

    /// <summary>
    /// Story was cancelled by the user or system.
    /// </summary>
    Cancelled
}
