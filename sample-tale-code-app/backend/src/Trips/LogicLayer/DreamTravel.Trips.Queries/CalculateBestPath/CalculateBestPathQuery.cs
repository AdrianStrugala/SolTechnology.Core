using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.Trips.Queries.CalculateBestPath
{
    public class CalculateBestPathQuery
    {
        public List<City?> Cities { get; set; }

        public CalculateBestPathQuery()
        {
            Cities = new List<City?>();
        }
    }
}
