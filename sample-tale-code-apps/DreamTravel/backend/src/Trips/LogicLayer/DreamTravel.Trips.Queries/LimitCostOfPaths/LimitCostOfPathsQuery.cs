using MediatR;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Trips.Queries.LimitCostOfPaths
{
    public class LimitCostOfPathsQuery : IRequest<Result<List<Path>>>
    {
        public int CostLimit { get; set; }
        public List<Path> Paths { get; set; } = null!;
    }
}
