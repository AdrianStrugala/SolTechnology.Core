﻿﻿﻿using DreamTravel.DomainServices.CityDomain;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Events;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries.FindCityByCoordinates
{
    public class FindCityByCoordinatesHandler(
        ICityDomainService cityDomainService,
        IMediator mediator)
        : IQueryHandler<FindCityByCoordinatesQuery, City>
    {
        public async Task<Result<City>> Handle(FindCityByCoordinatesQuery query, CancellationToken cancellationToken)
        {
            var result = await cityDomainService.Get(query.Lat, query.Lng);

            mediator.Publish(new CitySearched{ City = result });

            return result;
        }
    }
}
