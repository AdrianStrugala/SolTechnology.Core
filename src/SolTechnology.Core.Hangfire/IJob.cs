namespace SolTechnology.Core.Hangfire;

/// <summary>
/// A unit of work run on a cron schedule by Hangfire's recurring-job server.
/// Distinct from <see cref="CQRS.IEvent"/> — a job is pull/scheduled, an event is push/reactive.
/// </summary>
public interface IJob
{
    Task Execute(CancellationToken cancellationToken);
}

