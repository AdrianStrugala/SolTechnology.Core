using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public interface ICostLimitBreaker
    {
        List<Path> Handle(int costLimit, List<Path> paths);
    }
}
