using System.Collections.Generic;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Domain.Paths;

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
