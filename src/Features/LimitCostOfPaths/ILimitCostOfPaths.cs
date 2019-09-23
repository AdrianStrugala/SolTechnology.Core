using System.Collections.Generic;
using DreamTravel.Domain.Paths;

namespace DreamTravel.Features.LimitCostOfPaths
{
    public interface ILimitCostOfPaths
    {
        List<Path> Execute(int costLimit, List<Path> paths);
    }
}
