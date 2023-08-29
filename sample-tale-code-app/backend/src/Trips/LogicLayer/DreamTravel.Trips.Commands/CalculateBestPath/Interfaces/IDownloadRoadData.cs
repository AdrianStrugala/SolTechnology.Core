using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;

namespace DreamTravel.DreamTrips.CalculateBestPath.Interfaces
{
    public interface IDownloadRoadData
    {
        Task<EvaluationMatrix> Execute(List<City> listOfCities);
    }
}
