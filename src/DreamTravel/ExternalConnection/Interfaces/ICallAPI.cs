using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection.Interfaces
{
    public interface ICallAPI
    {
        (double, double) DowloadCostBetweenTwoCities(City origin, City destination);
        double[] DowloadDurationMatrixByTollRoad(List<City> listOfCities);
        double[] DowloadDurationMatrixByFreeRoad(List<City> listOfCities);
    }
}