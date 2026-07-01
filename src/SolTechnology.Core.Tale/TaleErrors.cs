namespace SolTechnology.Core.Tale;

/// <summary>
/// Marker error indicating that a story has paused at an interactive chapter
/// and is awaiting external input.
/// </summary>
public sealed record TalePausedError : Error
{
    public const string DefaultMessage = "Tale paused waiting for user input.";

    public TalePausedError(Auid storyId, string? chapterId = null)
    {
        TaleId = storyId;
        ChapterId = chapterId;
        Message = DefaultMessage;
        Recoverable = true;
    }

    public Auid TaleId { get; }
    public string? ChapterId { get; }
}

/// <summary>
/// Marker error indicating that a story was cancelled.
/// </summary>
public sealed record TaleCancelledError : Error
{
    public TaleCancelledError(Auid storyId)
    {
        TaleId = storyId;
        Message = "Tale execution cancelled.";
        Recoverable = false;
    }

    public Auid TaleId { get; }
}
