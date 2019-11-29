using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.DreamTrips.CalculateBestPath.Models;

namespace DreamTravel.DreamTrips.CalculateBestPath.Interfaces
{
    public interface IDownloadRoadData
    {
        Task<EvaluationMatrix> Execute(List<City> cities, EvaluationMatrix evaluationMatrix);
    }
}