namespace DreamTravel.BestPath
{
    using SharedModels;
    using System.Collections.Generic;

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
