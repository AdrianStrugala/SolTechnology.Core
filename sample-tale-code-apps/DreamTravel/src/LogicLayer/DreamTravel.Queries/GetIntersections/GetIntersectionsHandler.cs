using DreamTravel.Domain.StreetGraph;
using DreamTravel.GraphDatabase.Repositories;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries.GetIntersections;

public class GetIntersectionsHandler(IIntersectionRepository intersections)
    : IQueryHandler<GetIntersectionsQuery, List<Intersection>>
{
    public async Task<Result<List<Intersection>>> Handle(GetIntersectionsQuery query, CancellationToken cancellationToken)
    {
        var all = await intersections.GetAllAsync();
        return all.ToList();
    }
}
