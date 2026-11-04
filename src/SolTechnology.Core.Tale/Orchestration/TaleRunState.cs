using System.Text.Json;
using SolTechnology.Core.Tale.Models;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Mutable state of one tale run: chapter history, errors, the pending resume input, and the
/// paused / failed / cancelled flags that decide which track the tale is on.
/// </summary>
internal sealed class TaleRunState(TimeProvider timeProvider)
{
    public List<ChapterInfo> History { get; } = [];

    public List<Error> Errors { get; } = [];

    public JsonElement? ResumeInput { get; set; }

    public bool IsPaused { get; set; }

    public bool HasFailed { get; private set; }

    public bool IsCancelled { get; set; }

    public Error? SaveError { get; set; }

    // Won-track steps act only while the tale is still succeeding; lost-track steps only after a
    // failure (until an Otherwise recovers). Both stop once the tale is cancelled, paused or unsaved.
    public bool OnWonTrack => !IsCancelled && !IsPaused && !HasFailed && SaveError is null;

    public bool OnLostTrack => !IsCancelled && !IsPaused && HasFailed && SaveError is null;

    public TaleStatus IntermediateStatus =>
        IsCancelled ? TaleStatus.Cancelled
        : IsPaused ? TaleStatus.WaitingForInput
        : HasFailed ? TaleStatus.Failed
        : TaleStatus.Running;

    public TaleStatus TerminalStatus =>
        IsCancelled ? TaleStatus.Cancelled
        : HasFailed ? TaleStatus.Failed
        : IsPaused ? TaleStatus.WaitingForInput
        : TaleStatus.Completed;

    public DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    /// <summary>
    /// Finds the history entry of the chapter at <paramref name="stepIndex"/>, or adds a new one.
    /// Returns <c>null</c> when that chapter already completed; resumes a tale paused at it.
    /// </summary>
    public ChapterInfo? Track(string chapterId, string chapterTypeName, int stepIndex, out bool resumed)
    {
        var existing = History.FirstOrDefault(h => h.StepIndex == stepIndex && h.ChapterTypeName == chapterTypeName);
        resumed = IsPaused && existing?.Status == TaleStatus.WaitingForInput;
        if (resumed)
        {
            IsPaused = false;
        }

        if (existing != null)
        {
            return existing.Status == TaleStatus.Completed ? null : existing;
        }

        var chapterInfo = new ChapterInfo
        {
            ChapterId = chapterId,
            StepIndex = stepIndex,
            ChapterTypeName = chapterTypeName,
            StartedAt = Now,
            Status = TaleStatus.Running
        };
        History.Add(chapterInfo);
        return chapterInfo;
    }

    public void Fail(ChapterInfo? chapterInfo, Error error)
    {
        Errors.Add(error);
        HasFailed = true;
        if (chapterInfo != null)
        {
            chapterInfo.Error = error;
            chapterInfo.Status = TaleStatus.Failed;
            chapterInfo.FinishedAt = Now;
        }
    }

    public void ClearFailure()
    {
        HasFailed = false;
        Errors.Clear();
    }

    public void Restore(TaleInstance instance)
    {
        History.Clear();
        History.AddRange(instance.History);
        IsPaused = instance.Status == TaleStatus.WaitingForInput;
        HasFailed = instance.Status == TaleStatus.Failed;
        IsCancelled = instance.Status == TaleStatus.Cancelled;
    }
}
