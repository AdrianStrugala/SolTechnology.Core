using System.Collections.Generic;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Paths;

namespace DreamTravel.DreamTrips.CalculateBestPath
{
    public class Result
    {
        public List<Path> BestPaths { get; set; }
        public List<City> Cities { get; set; }

        public Result()
        {
            BestPaths = new List<Path>();
            Cities = new List<City>();
        }
    }
}
