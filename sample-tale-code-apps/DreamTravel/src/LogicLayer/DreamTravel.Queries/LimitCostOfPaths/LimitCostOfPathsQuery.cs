using MediatR;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.Queries.LimitCostOfPaths
{
    public class LimitCostOfPathsQuery : IRequest<Result<List<Path>>>
    {
        public int CostLimit { get; set; }
        public List<Path> Paths { get; set; } = null!;
    }
}
