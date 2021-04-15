using System.Collections.Generic;
using DreamTravel.Domain.Paths;

namespace DreamTravel.DreamTrips.LimitCostOfPaths
{
    public class LimitCostsOfPathsQuery
    {
        public int CostLimit { get; set; }
        public List<Path> Paths { get; set; }
    }
}
