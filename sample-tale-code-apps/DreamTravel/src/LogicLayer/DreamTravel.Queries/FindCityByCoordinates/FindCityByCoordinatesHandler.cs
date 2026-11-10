﻿﻿﻿using DreamTravel.DomainServices.CityDomain;
using DreamTravel.Domain.Cities;
using DreamTravel.Queries.Events;
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
            if (result.IsFailure)
            {
                return result;
            }

            mediator.Publish(new CitySearched{ City = result.Data! });

            return result;
        }
    }
}
