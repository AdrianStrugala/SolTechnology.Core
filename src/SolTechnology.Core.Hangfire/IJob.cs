namespace SolTechnology.Core.Hangfire;

/// <summary>
/// A unit of work run on a cron schedule by Hangfire's recurring-job server.
/// Distinct from <see cref="CQRS.IEvent"/> — a job is pull/scheduled, an event is push/reactive.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Runs one occurrence of the job. A failed <see cref="Result"/> whose <see cref="Error.Recoverable"/> is
    /// <c>true</c> is retried a bounded number of times when <c>UseSolFilters</c> is called; a thrown exception is
    /// not retried.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the Hangfire server shuts down.</param>
    Task<Result> Execute(CancellationToken cancellationToken);
}
