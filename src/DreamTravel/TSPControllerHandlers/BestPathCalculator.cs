using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using TravelingSalesmanProblem;

namespace DreamTravel.TSPControllerHandlers
{
    public class BestPathCalculator : IBestPathCalculator
    {
        private readonly IProcessInputData _processInputData;
        private readonly IProcessOutputData _processOutputData;
        private readonly ITSP _TSPSolver;

        public BestPathCalculator(IProcessInputData processInputData, IProcessOutputData processOutputData, ITSP TSPSolver)
        {
            _processInputData = processInputData;
            _processOutputData = processOutputData;
            _TSPSolver = TSPSolver;
        }

        public async Task<List<Path>> Handle(string cities)
        {
            List<string> listOfCitiesAsStrings = _processInputData.ReadCities(cities);
            EvaluationMatrix matrices = new EvaluationMatrix(listOfCitiesAsStrings.Count);
            var listOfCities = await _processInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);
            matrices = await _processInputData.DownloadExternalData(listOfCities, matrices);
            matrices = await _processInputData.EvaluateCostAsync(matrices, listOfCities.Count);
            int[] orderOfCities = _TSPSolver.SolveTSP(matrices.OptimalDistances);

            return _processOutputData.FormOutputFromTSPResult(listOfCities, orderOfCities, matrices);
        }

    }
}
