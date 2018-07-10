using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public interface IBestPathCalculator
    {
        List<Path> Handle(string cities);
    }
}