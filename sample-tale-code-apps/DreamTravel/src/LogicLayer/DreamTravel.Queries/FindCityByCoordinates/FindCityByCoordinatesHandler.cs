using DreamTravel.DomainServices.CityDomain;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Events;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Jobs;

namespace DreamTravel.Queries.FindCityByCoordinates
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
