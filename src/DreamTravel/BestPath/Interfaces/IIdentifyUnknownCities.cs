namespace DreamTravel.BestPath.Interfaces
{
    using SharedModels;
    using System.Collections.Generic;

    public interface IIdentifyUnknownCities
    {
        List<City> Execute(List<City> newCities, List<City> knownCities);
    }
}
