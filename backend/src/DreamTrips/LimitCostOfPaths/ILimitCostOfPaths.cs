using System.Collections.Generic;
using DreamTravel.Domain.Paths;

namespace DreamTravel.DreamTrips.LimitCostOfPaths
{
    public interface ILimitCostOfPaths
    {
        List<Path> Handle(int costLimit, List<Path> paths);
    }
}
