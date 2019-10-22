using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.Features.DreamTrip.CalculateBestPath.Models;

namespace DreamTravel.Features.DreamTrip.CalculateBestPath.Interfaces
{
    public interface IDownloadRoadData
    {
        Task<EvaluationMatrix> Execute(List<City> cities, EvaluationMatrix evaluationMatrix);
    }
}