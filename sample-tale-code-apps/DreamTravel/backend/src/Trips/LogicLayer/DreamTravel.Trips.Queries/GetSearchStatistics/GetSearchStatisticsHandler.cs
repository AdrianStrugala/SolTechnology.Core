using DreamTravel.Trips.Domain.SearchStatistics;
using DreamTravel.Trips.Sql.Repositories;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.GetSearchStatistics;

public class GetSearchStatisticsHandler(
    ICityStatisticsRepository cityStatisticsRepository,
    ICountryStatisticsRepository countryStatisticsRepository) : IQueryHandler<GetSearchStatisticsQuery, GetSearchStatisticsResult>
{
    public async Task<Result<GetSearchStatisticsResult>> Handle(GetSearchStatisticsQuery request, CancellationToken cancellationToken)
    {
        var countryStatistics = await countryStatisticsRepository.GetAll();
        var cityStatistics = await cityStatisticsRepository.GetAll();
        
        var result = new GetSearchStatisticsResult
        {
            CountryStatistics = countryStatistics.Select(c => new CountryStatistics
            {
                Country = c.Country,
                TotalSearchCount = c.TotalSearchCount,
                CityStatistics = cityStatistics
                    .Where(x => x.Country == c.Country)
                    .ToList()
            })
            .ToList()
        };

        return result;
    }
    
}


