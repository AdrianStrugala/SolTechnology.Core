namespace SolTechnology.Core.Tale;

/// <summary>
/// Marker error indicating that a tale has paused at an interactive chapter
/// and is awaiting external input.
/// </summary>
public sealed record TalePausedError : Error
{
    public const string DefaultMessage = "Tale paused waiting for user input.";

    public TalePausedError(Auid taleId, string? chapterId = null)
    {
        TaleId = taleId;
        ChapterId = chapterId;
        Message = DefaultMessage;
        Recoverable = true;
    }

    public Auid TaleId { get; }
    public string? ChapterId { get; }
}

/// <summary>
/// Marker error indicating that a tale was cancelled.
/// </summary>
public sealed record TaleCancelledError : Error
{
    public TaleCancelledError(Auid taleId)
    {
        TaleId = taleId;
        Message = "Tale execution cancelled.";
        Recoverable = false;
    }

    public Auid TaleId { get; }
}
