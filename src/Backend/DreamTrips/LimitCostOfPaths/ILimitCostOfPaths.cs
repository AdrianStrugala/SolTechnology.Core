using System.Collections.Generic;
using DreamTravel.Domain.Paths;

namespace DreamTravel.DreamTrips.LimitCostOfPaths
{
    public interface ILimitCostOfPaths
    {
        List<Path> Execute(int costLimit, List<Path> paths);
    }
}
