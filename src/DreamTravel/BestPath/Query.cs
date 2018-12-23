namespace DreamTravel.BestPath
{
    using SharedModels;
    using System.Collections.Generic;

    public class Query
    {
        public List<City> Cities { get; set; }
        public string SessionId { get; set; }
        public bool OptimizePath { get; set; }

        public Query()
        {
            Cities = new List<City>();
        }
    }
}
