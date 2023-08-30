using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Trips.Queries.LimitCostOfPaths
{
    public class LimitCostOfPathsInput
    {
        public int CostLimit { get; set; }
        public List<Path> Paths { get; set; }
    }
}
