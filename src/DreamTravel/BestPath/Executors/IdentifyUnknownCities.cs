namespace DreamTravel.BestPath.Executors
{
    using Interfaces;
    using SharedModels;
    using System.Collections.Generic;
    using System.Linq;

    public class IdentifyUnknownCities : IIdentifyUnknownCities
    {

        public List<City> Execute(List<City> newCities, List<City> knownCities)
        {
            var result = newCities.Except(knownCities).ToList();

            return result;
        }
    }
}
