namespace SolTechnology.Core.Story;

/// <summary>
/// Marker error indicating that a story has paused at an interactive chapter
/// and is awaiting external input.
/// </summary>
public sealed record StoryPausedError : Error
{
    public const string DefaultMessage = "Story paused waiting for user input.";

    public StoryPausedError(Auid storyId, string? chapterId = null)
    {
        StoryId = storyId;
        ChapterId = chapterId;
        Message = DefaultMessage;
        Recoverable = true;
    }

    public Auid StoryId { get; }
    public string? ChapterId { get; }
}

/// <summary>
/// Marker error indicating that a story was cancelled.
/// </summary>
public sealed record StoryCancelledError : Error
{
    public StoryCancelledError(Auid storyId)
    {
        StoryId = storyId;
        Message = "Story execution cancelled.";
        Recoverable = false;
    }

    public Auid StoryId { get; }
}
