using DreamTravel.Trips.Commands.FetchTraffic;
using Hangfire;

namespace DreamTravel.Worker.BackgroundJobs;

public static class FetchTrafficJob
{
    public static void Register()
    {
        RecurringJob.AddOrUpdate<FetchTrafficHandler>(
            "traffic-regular-update",
            handler => handler.Handle(new FetchTrafficCommand
            {
                DepartureTime = new DateTime(2025, 10, 1, 14, 0, 0)
            }, CancellationToken.None),
            Cron.Never
        );
    }
}