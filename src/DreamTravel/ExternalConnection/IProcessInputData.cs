using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public interface IProcessInputData
    {
        Task<List<City>> GetCitiesFromGoogleApi(List<string> cityNames);

        List<string> ReadCities(string[] incomingCities);

        EvaluationMatrix DownloadExternalData(List<City> listOfCities, EvaluationMatrix evaluationMatrix);
    }
}