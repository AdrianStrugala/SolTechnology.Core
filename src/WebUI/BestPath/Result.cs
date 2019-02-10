namespace WebUI.BestPath
{
    using System.Collections.Generic;
    using SharedModels;

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
