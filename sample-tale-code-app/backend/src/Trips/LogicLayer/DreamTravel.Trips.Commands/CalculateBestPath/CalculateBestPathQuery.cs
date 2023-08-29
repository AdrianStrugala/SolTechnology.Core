using System.Collections.Generic;
using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.DreamTrips.CalculateBestPath
{
    public class CalculateBestPathQuery
    {
        public List<City> Cities { get; set; }

        public CalculateBestPathQuery()
        {
            Cities = new List<City>();
        }
    }
}
