using DreamTravel.DomainServices.CityDomain;
using DreamTravel.Infrastructure.Events;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Domain.Events;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.FindCityByCoordinates
{
    public class FindCityByCoordinatesHandler(
        ICityDomainService cityDomainService,
        IHangfireNotificationPublisher notificationPublisher)
        : IQueryHandler<FindCityByCoordinatesQuery, City>
    {
        public async Task<Result<City>> Handle(FindCityByCoordinatesQuery query, CancellationToken cancellationToken)
        {
            var result = await cityDomainService.Get(query.Lat, query.Lng);

            notificationPublisher.Publish(new CitySearched{ City = result });
            
            return result;
        }
    }
}
