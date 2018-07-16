using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public interface IProcessInputData
    {
        Task<List<City>> GetCitiesFromGoogleApi(List<string> cityNames);

        Task<double> GetCostBetweenTwoCities(City origin, City destination);

        Task<int> GetDurationBetweenTwoCitiesByFreeRoad(City origin, City destination);

        Task<int> GetDurationBetweenTwoCitiesByTollRoad(City origin, City destination);

        List<string> ReadCities(string incomingCities);

        Task<EvaluationMatrix> DownloadExternalData(List<City> listOfCities, EvaluationMatrix evaluationMatrix);
        Task<EvaluationMatrix> EvaluateCostAsync(EvaluationMatrix evaluationMatrix, int noOfCities);
    }
}