using DreamTravel.Trips.Commands.FetchCity;
using DreamTravel.Trips.Domain.Events;
using MediatR;

namespace DreamTravel.Worker.EventHandlers.OnCitySearched;

public class FetchCityDetailsJob(IMediator mediator) : INotificationHandler<CitySearched>
{
    public async Task Handle(CitySearched notification, CancellationToken cancellationToken)
    {
        await mediator.Send(new FetchCityDetailsCommand{Name = notification.Name}, cancellationToken);
    }
}