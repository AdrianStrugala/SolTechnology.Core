using System.Collections.Concurrent;
using Hangfire.Common;
using Hangfire.Server;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

/// <summary>An async job body that records one Chronicle step after a thread hop.</summary>
public sealed class ChronicleProbeJob(IChronicle chronicle)
{
    public async Task Run(string step)
    {
        await Task.Delay(1);
        using (chronicle.Step(step))
        {
            await Task.Delay(1);
        }
    }
}

/// <summary>Ordered after <c>ChronicleJobFilter</c> (Order 0) and throws before the job runs (CRM-2).</summary>
public sealed class ThrowingFilter : IServerFilter, IJobFilter
{
    public bool AllowMultiple => false;

    public int Order => 1;

    public void OnPerforming(PerformingContext filterContext) => throw new InvalidOperationException("later filter failed");

    public void OnPerformed(PerformedContext filterContext)
    {
    }
}

public class NonRecoverableFailingJob : IJob
{
    public Task<Result> Execute(CancellationToken cancellationToken) =>
        Result.FailAsTask(new NotFoundError { Message = "City [Atlantis] not found", Recoverable = false });
}

public sealed record ShutdownEvent : IEvent;

public sealed record ProbeEvent : IEvent;

internal sealed class WaitingHandler : IEventHandler<ShutdownEvent>
{
    public Task Handle(ShutdownEvent notification, CancellationToken cancellationToken) =>
        Task.Delay(Timeout.Infinite, cancellationToken);
}

internal sealed class RecordingHandler(ConcurrentQueue<string> handled) : IEventHandler<ShutdownEvent>
{
    public Task Handle(ShutdownEvent notification, CancellationToken cancellationToken)
    {
        handled.Enqueue("recorded");
        return Task.CompletedTask;
    }
}
