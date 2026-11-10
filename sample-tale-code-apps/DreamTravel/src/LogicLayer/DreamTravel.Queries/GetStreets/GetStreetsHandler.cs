using DreamTravel.Domain.StreetGraph;
using DreamTravel.GraphDatabase.Repositories;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries.GetStreets;

public class GetStreetsHandler(IStreetRepository streets)
    : IQueryHandler<GetStreetsQuery, List<Street>>
{
    public async Task<Result<List<Street>>> Handle(GetStreetsQuery query, CancellationToken cancellationToken)
    {
        return await streets.GetAllAsync();
    }
}
