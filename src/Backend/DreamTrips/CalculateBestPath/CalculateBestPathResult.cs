using System.Collections.Generic;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Paths;

namespace DreamTravel.DreamTrips.CalculateBestPath
{
    public class CalculateBestPathResult
    {
        public List<Path> BestPaths { get; set; }
        public List<City> Cities { get; set; }

        public CalculateBestPathResult()
        {
            BestPaths = new List<Path>();
            Cities = new List<City>();
        }
    }
}
