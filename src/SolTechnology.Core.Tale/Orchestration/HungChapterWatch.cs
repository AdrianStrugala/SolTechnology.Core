using Microsoft.Extensions.Logging;
using SolTechnology.Core.AUID;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Warns once per chapter when a single chapter of a persisted run has been executing for three
/// <see cref="TaleOptions.RunningLeaseTimeout"/>s. The heartbeat keeps renewing the lease of such a
/// chapter, so only this warning tells an operator that it may hang and needs <c>CancelStory</c>.
/// </summary>
internal sealed class HungChapterWatch(TimeProvider clock, TimeSpan runningLeaseTimeout, ILogger logger, Auid taleId)
{
    private readonly Lock _lock = new();
    private readonly TimeSpan _threshold = runningLeaseTimeout * 3;
    private string? _chapterId;
    private DateTimeOffset _startedAt;
    private bool _warned;

    public void Enter(string chapterId)
    {
        lock (_lock)
        {
            _chapterId = chapterId;
            _startedAt = clock.GetUtcNow();
            _warned = false;
        }
    }

    public void Leave()
    {
        lock (_lock)
        {
            _chapterId = null;
        }
    }

    public void Check()
    {
        string chapterId;
        TimeSpan elapsed;
        lock (_lock)
        {
            elapsed = clock.GetUtcNow() - _startedAt;
            if (_chapterId == null || _warned || elapsed < _threshold)
            {
                return;
            }

            _warned = true;
            chapterId = _chapterId;
        }

        logger.LogWarning(
            "Tale [{TaleId}] chapter [{ChapterId}] has been running for [{Elapsed}], three Running lease timeouts; " +
            "the lease proves the host is alive, not that the chapter progresses, so recover a hung chapter with CancelStory",
            taleId, chapterId, elapsed);
    }
}
