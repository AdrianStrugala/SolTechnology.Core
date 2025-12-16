using DreamTravel.Domain.Events;
using Hangfire.Annotations;
using MediatR;

namespace DreamTravel.Worker.EventHandlers.OnCitySearched;

[UsedImplicitly]
public class LogEventInfoJob(ILogger<LogEventInfoJob> logger) : INotificationHandler<CitySearched>
{
    public Task Handle(CitySearched notification, CancellationToken cancellationToken)
    {
        logger.LogInformation($"{nameof(CitySearched)}. DUPA.");
        return Task.CompletedTask;
    }
}