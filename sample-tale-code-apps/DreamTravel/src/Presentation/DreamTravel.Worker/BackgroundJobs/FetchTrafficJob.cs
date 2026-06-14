using DreamTravel.Commands.FetchTraffic;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Hangfire;

namespace DreamTravel.Worker.BackgroundJobs;

public class FetchTrafficJob(IMediator mediator) : IJob
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        await mediator.Send(new FetchTrafficCommand
        {
            DepartureTime = new DateTime(2025, 10, 1, 14, 0, 0, DateTimeKind.Utc)
        }, cancellationToken);
    }
}
