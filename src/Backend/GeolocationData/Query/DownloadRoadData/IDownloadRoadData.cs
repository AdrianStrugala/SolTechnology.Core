using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.GeolocationData.Query.DownloadRoadData
{
    public interface IDownloadRoadData
    {
        Task<EvaluationMatrix> Execute(List<City> listOfCities,
                                       EvaluationMatrix evaluationMatrix);
    }
}
