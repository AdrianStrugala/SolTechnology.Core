using DreamTravel.DomainServices.CityDomain;
using DreamTravel.Domain.Cities;
using DreamTravel.Queries.Events;
using DreamTravel.GeolocationDataClients.GoogleApi;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Logging.Chronicles;

namespace DreamTravel.Queries.FindCityByName
{
    public class FindCityByNameHandler(
        ICityDomainService cityDomainService,
        IMediator mediator,
        IChronicle chronicle,
        ILogger<FindCityByNameHandler> logger)
        : IQueryHandler<FindCityByNameQuery, City>
    {
        public async Task<Result<City>> Handle(FindCityByNameQuery query, CancellationToken cancellationToken)
        {
            City result;

            using (chronicle.Step("FetchCity"))
            {
                result = await cityDomainService.Get(query.Name);
            }

            using (chronicle.Step("FetchCityCached"))
            {
                //to test cache
                result = await cityDomainService.Get(query.Name);
            }

            mediator.Publish(new CitySearched { City = result });

            return result;
        }
    }
}
