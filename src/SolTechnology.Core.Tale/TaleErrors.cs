using SolTechnology.Core.AUID;

namespace SolTechnology.Core.Tale;

/// <summary>
/// Marker error indicating that a tale has paused at an interactive chapter
/// and is awaiting external input.
/// </summary>
public sealed record TalePausedError : Error
{
    /// <summary>Message of every <see cref="TalePausedError"/>.</summary>
    public const string DefaultMessage = "Tale paused waiting for user input.";

    /// <summary>Creates the marker for a tale paused at <paramref name="chapterId"/>.</summary>
    /// <param name="taleId">The paused tale.</param>
    /// <param name="chapterId">Display ID of the chapter waiting for input.</param>
    public TalePausedError(Auid taleId, string? chapterId = null)
    {
        TaleId = taleId;
        ChapterId = chapterId;
        Message = DefaultMessage;
        Recoverable = true;
    }

    /// <summary>The paused tale.</summary>
    public Auid TaleId { get; }

    /// <summary>Display ID of the chapter waiting for input.</summary>
    public string? ChapterId { get; }
}

/// <summary>
/// Marker error indicating that a tale was cancelled.
/// </summary>
public sealed record TaleCancelledError : Error
{
    /// <summary>Creates the marker for a cancelled tale.</summary>
    /// <param name="taleId">The cancelled tale.</param>
    public TaleCancelledError(Auid taleId)
    {
        TaleId = taleId;
        Message = "Tale execution cancelled.";
        Recoverable = false;
    }

    /// <summary>The cancelled tale.</summary>
    public Auid TaleId { get; }
}
