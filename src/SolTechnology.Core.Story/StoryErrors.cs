using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.Story;

/// <summary>
/// Marker error indicating that a story has paused at an interactive chapter
/// and is awaiting external input. This is not a true failure — callers that
/// understand the Story Framework (e.g. <c>StoryManager</c>) should treat it
/// as a legitimate terminal state for the current invocation.
/// </summary>
/// <remarks>
/// Use <c>result.Error is StoryPausedError</c> to detect pause, instead of
/// string matching on <see cref="Error.Message"/>.
/// </remarks>
public sealed class StoryPausedError : Error
{
    public const string DefaultMessage = "Story paused waiting for user input.";

    public StoryPausedError(Auid storyId, string? chapterId = null)
    {
        StoryId = storyId;
        ChapterId = chapterId;
        Message = DefaultMessage;
        Recoverable = true;
    }

    /// <summary>Story instance that is paused.</summary>
    public Auid StoryId { get; }

    /// <summary>Chapter at which the story paused, if known.</summary>
    public string? ChapterId { get; }
}

/// <summary>
/// Marker error indicating that a story was cancelled (via <see cref="CancellationToken"/>
/// or an explicit cancel request).
/// </summary>
public sealed class StoryCancelledError : Error
{
    public StoryCancelledError(Auid storyId)
    {
        StoryId = storyId;
        Message = "Story execution cancelled.";
        Recoverable = false;
    }

    public Auid StoryId { get; }
}
