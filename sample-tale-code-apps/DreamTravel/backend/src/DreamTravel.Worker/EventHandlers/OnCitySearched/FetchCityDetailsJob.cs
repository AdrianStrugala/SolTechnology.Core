using DreamTravel.Trips.Commands.FetchCity;
using DreamTravel.Trips.Domain.Events;
using Hangfire.Annotations;
using MediatR;

namespace DreamTravel.Worker.EventHandlers.OnCitySearched;

[UsedImplicitly]
public class FetchCityDetailsJob(IMediator mediator) : INotificationHandler<CitySearched>
{
    public async Task Handle(CitySearched notification, CancellationToken cancellationToken)
    {
        await mediator.Send(new FetchCityDetailsCommand { Name = notification.Name }, cancellationToken);
    }
}