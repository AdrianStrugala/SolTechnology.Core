using DreamTravel.Commands.FetchTraffic;
using SolTechnology.Core;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Hangfire;

namespace DreamTravel.Worker.BackgroundJobs;

public class FetchTrafficJob(IMediator mediator) : IJob
{
    public Task<Result> Execute(CancellationToken cancellationToken) =>
        mediator.Send(new FetchTrafficCommand
        {
            DepartureTime = new DateTime(2025, 10, 1, 14, 0, 0, DateTimeKind.Utc)
        }, cancellationToken);
}
