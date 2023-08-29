using System.Collections.Generic;
using DreamTravel.Trips.Domain.Paths;

namespace DreamTravel.DreamTrips.LimitCostOfPaths
{
    public class LimitCostOfPathsInput
    {
        public int CostLimit { get; set; }
        public List<Path> Paths { get; set; }
    }
}
