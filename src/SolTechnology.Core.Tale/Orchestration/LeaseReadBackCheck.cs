using Microsoft.Extensions.Logging;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Detects an <see cref="ITaleRepository"/> that does not store <see cref="TaleInstance.LeaseExpiresAt"/>
/// as written. Once per host (one instance per DI container) for each <see cref="LeaseWrite"/>, it reads
/// the tale back through the repository that saved it and logs an error when the stored expiry differs
/// from the written one by more than one second. A read that returns no row or another version (a lagging
/// replica, a concurrent writer) skips the check. The check never fails the run.
/// </summary>
internal sealed class LeaseReadBackCheck
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(1);
    private readonly int[] _checked = new int[Enum.GetValues<LeaseWrite>().Length];

    public async Task Verify(
        ITaleRepository repository,
        TaleInstance written,
        LeaseWrite write,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _checked[(int)write], 1) == 1)
        {
            return;
        }

        TaleInstance? stored;
        try
        {
            stored = await repository.FindById(written.TaleId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Tale [{TaleId}] could not be read back to verify the stored LeaseExpiresAt after the [{LeaseWrite}]",
                written.TaleId, write);
            return;
        }

        if (stored == null || stored.Version != written.Version || IsSameInstant(stored.LeaseExpiresAt, written.LeaseExpiresAt))
        {
            return;
        }

        logger.LogError("Tale [{TaleId}] LeaseExpiresAt read back from [{Repository}] after the [{LeaseWrite}] is [{StoredLeaseExpiresAt}] but [{WrittenLeaseExpiresAt}] was written; ITaleRepository.Save must store LeaseExpiresAt unchanged, or another host may take over a live tale",
            written.TaleId, repository.GetType().Name, write, stored.LeaseExpiresAt, written.LeaseExpiresAt);
    }

    private static bool IsSameInstant(DateTimeOffset? stored, DateTimeOffset? written) => (stored, written) switch
    {
        (null, null) => true,
        ({ } storedAt, { } writtenAt) => (storedAt - writtenAt).Duration() <= Tolerance,
        _ => false
    };
}
