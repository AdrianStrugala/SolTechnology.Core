namespace DreamTravel.BestPath
{
    using System.Collections.Generic;
    using SharedModels;

    public class Command
    {
        public List<City> Cities { get; set; }
        public bool OptimizePath { get; set; }


        public Command()
        {
            Cities = new List<City>();
        }
    }
}
