using DreamTravel.DomainServices.CityDomain;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Events;
using DreamTravel.GeolocationDataClients.GoogleApi;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Logging;

namespace DreamTravel.Queries.FindCityByName
{
    public class FindCityByNameHandler(
        ICityDomainService cityDomainService,
        IMediator mediator,
        ITimingService timingService,
        ILogger<FindCityByNameHandler> logger)
        : IQueryHandler<FindCityByNameQuery, City>
    {
        public async Task<Result<City>> Handle(FindCityByNameQuery query, CancellationToken cancellationToken)
        {
            City result;

            using (timingService.StartContext("http"))
            {
                result = await cityDomainService.Get(query.Name);
            }

            using (timingService.StartContext("cache"))
            {
                //to test cache
                result = await cityDomainService.Get(query.Name);
            }

            mediator.Publish(new CitySearched { City = result });

            return result;
        }
    }
}
