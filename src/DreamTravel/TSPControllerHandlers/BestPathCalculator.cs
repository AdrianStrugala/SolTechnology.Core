using System.Collections.Generic;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using TravelingSalesmanProblem;

namespace DreamTravel.TSPControllerHandlers
{
    public class BestPathCalculator : IBestPathCalculator
    {
        public List<Path> CalculateBestPath(string cities, IProcessInputData processInputData, IProcessOutputData processOutputData, ITSP TSPSolver)
        {
            List<string> listOfCitiesAsStrings = processInputData.ReadCities(cities);
            EvaluationMatrix matrices = new EvaluationMatrix(listOfCitiesAsStrings.Count);
            var listOfCities = processInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);
            matrices = processInputData.FillMatrixWithData(listOfCities, matrices);
            int[] orderOfCities = TSPSolver.SolveTSP(matrices.OptimalDistances);

            return processOutputData.FormOutputFromTSPResult(listOfCities, orderOfCities, matrices);
        }

    }
}
