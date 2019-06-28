namespace DreamTravel.WebUI.BestPath
{
    using System.Collections.Generic;
    using Contract;

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
