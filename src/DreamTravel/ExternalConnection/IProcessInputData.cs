using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public interface IProcessInputData
    {
        List<City> GetCitiesFromGoogleApi(List<string> cityNames);

        double GetCostBetweenTwoCities(City origin, City destination);

        int GetDurationBetweenTwoCitiesByFreeRoad(City origin, City destination);

        int GetDurationBetweenTwoCitiesByTollRoad(City origin, City destination);

        List<string> ReadCities(string incomingCities);

        EvaluationMatrix FillMatrixWithData(List<City> listOfCities, EvaluationMatrix evaluationMatrix);
    }
}