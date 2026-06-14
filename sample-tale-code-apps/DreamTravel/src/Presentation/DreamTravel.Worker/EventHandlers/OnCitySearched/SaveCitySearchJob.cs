using DreamTravel.DomainServices.CityDomain;
using DreamTravel.Domain.Events;
using Hangfire.Annotations;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Worker.EventHandlers.OnCitySearched;

[UsedImplicitly]
public class SaveCitySearchJob(ICityDomainService cityDomainService) : IEventHandler<CitySearched>
{
    public async Task Handle(CitySearched notification, CancellationToken cancellationToken)
    {
        await cityDomainService.Save(notification.City);
    }
}
