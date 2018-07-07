using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public interface ICostLimitBreaker
    {
        List<Path> AdjustPaths(int costLimit, List<Path> paths);
    }
}
