using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.Features.CalculateBestPath.Models;

namespace DreamTravel.Features.CalculateBestPath.Interfaces
{
    public interface IDownloadRoadData
    {
        Task<EvaluationMatrix> Execute(List<City> cities, EvaluationMatrix evaluationMatrix);
    }
}