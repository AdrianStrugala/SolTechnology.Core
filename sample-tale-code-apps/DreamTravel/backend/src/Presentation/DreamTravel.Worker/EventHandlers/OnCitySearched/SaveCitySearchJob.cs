using DreamTravel.DomainServices.CityDomain;
using DreamTravel.Trips.Domain.Events;
using Hangfire.Annotations;
using MediatR;

namespace DreamTravel.Worker.EventHandlers.OnCitySearched;

[UsedImplicitly]
public class SaveCitySearchJob(ICityDomainService cityDomainService) : INotificationHandler<CitySearched>
{
    public async Task Handle(CitySearched notification, CancellationToken cancellationToken)
    {
        await cityDomainService.Save(notification.City);
    }
}