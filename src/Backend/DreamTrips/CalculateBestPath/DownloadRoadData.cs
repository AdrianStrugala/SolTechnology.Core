using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Matrices;
using DreamTravel.DreamTrips.CalculateBestPath.Interfaces;
using DreamTravel.DreamTrips.CalculateBestPath.Models;

namespace DreamTravel.DreamTrips.CalculateBestPath
{
    public class DownloadRoadData : IDownloadRoadData
    {
        private readonly IMatrixRepository _matrixRepository;

        public DownloadRoadData(IMatrixRepository matrixRepository)
        {
            _matrixRepository = matrixRepository;
        }

        public async Task<EvaluationMatrix> Execute(List<City> cities, EvaluationMatrix evaluationMatrix)
        {
            var downloadTollDistanceTask = _matrixRepository.GetTollRoadDuration(cities);
            var downloadFreeDistanceTask = _matrixRepository.GetFreeRoadDuration(cities);
            var downloadCostsTask = _matrixRepository.GetCosts(cities);

            evaluationMatrix.TollDistances = await downloadTollDistanceTask;
            evaluationMatrix.FreeDistances = await downloadFreeDistanceTask;
            (evaluationMatrix.Costs, evaluationMatrix.VinietaCosts) = await downloadCostsTask;
            
            return evaluationMatrix;
        }
    }
}