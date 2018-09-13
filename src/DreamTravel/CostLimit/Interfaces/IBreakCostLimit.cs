using System.Collections.Generic;
using DreamTravel.SharedModels;

namespace DreamTravel.CostLimit.Interfaces
{
    public interface IBreakCostLimit
    {
        List<Path> Execute(int costLimit, List<Path> paths);
    }
}
