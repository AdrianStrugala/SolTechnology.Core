using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.Features.CalculateBestPath.Interfaces;
using DreamTravel.Features.CalculateBestPath.Models;
using DreamTravel.GeolocationData;

namespace DreamTravel.Features.CalculateBestPath
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
            List<Task> tasks = new List<Task>
            {
                Task.Run(async () => evaluationMatrix.TollDistances = await _matrixRepository.GetTollRoadDuration(cities)),
                Task.Run(async () => evaluationMatrix.FreeDistances = await _matrixRepository.GetFreeRoadDuration(cities)),
                Task.Run(async () => (evaluationMatrix.Costs, evaluationMatrix.VinietaCosts) = await _matrixRepository.GetCosts(cities))
            };

            await Task.WhenAll(tasks);

            return evaluationMatrix;
        }
    }
}