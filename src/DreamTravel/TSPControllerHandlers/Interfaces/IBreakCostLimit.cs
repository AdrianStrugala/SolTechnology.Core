using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers.Interfaces
{
    public interface IBreakCostLimit
    {
        List<Path> Execute(int costLimit, List<Path> paths);
    }
}
