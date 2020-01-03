using System.Collections.Generic;
using DreamTravel.Domain.Cities;

namespace DreamTravel.DreamTrips.CalculateBestPath
{
    public class Query
    {
        public List<City> Cities { get; set; }
        public string SessionId { get; set; }

        public Query()
        {
            Cities = new List<City>();
        }
    }
}
