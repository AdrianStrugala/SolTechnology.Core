using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public interface IBreakCostLimit
    {
        List<Path> Execute(int costLimit, List<Path> paths);
    }
}
