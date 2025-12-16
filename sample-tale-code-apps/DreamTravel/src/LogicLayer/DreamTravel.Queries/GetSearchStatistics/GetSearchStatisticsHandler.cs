using DreamTravel.Sql;
using Microsoft.EntityFrameworkCore;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries.GetSearchStatistics;

public class GetSearchStatisticsHandler(
    DreamTripsDbContext dbContext) : IQueryHandler<GetSearchStatisticsQuery, GetSearchStatisticsResult>
{
    public async Task<Result<GetSearchStatisticsResult>> Handle(GetSearchStatisticsQuery request, CancellationToken cancellationToken)
    {
        var countryStatistics = await dbContext.CountryStatistics.ToListAsync(cancellationToken: cancellationToken);
        
        var result = new GetSearchStatisticsResult
        {
            CountryStatistics = countryStatistics.Select(c => new CountryStatistics
            {
                Country = c.Country,
                TotalSearchCount = c.TotalSearchCount
            })
            .ToList()
        };

        return result;
    }
    
}


