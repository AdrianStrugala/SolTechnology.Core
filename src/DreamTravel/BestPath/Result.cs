namespace DreamTravel.BestPath
{
    using Models;
    using SharedModels;
    using System.Collections.Generic;

    public class Result
    {
        public List<Path> BestPaths { get; set; }
        public List<City> Cities { get; set; }
        public List<Path> AllPaths { get; set; }

        public Result()
        {
            BestPaths = new List<Path>();
            AllPaths = new List<Path>();
            Cities = new List<City>();
        }
    }
}
