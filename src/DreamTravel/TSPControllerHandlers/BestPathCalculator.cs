using System.Collections.Generic;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public class BestPathCalculator
    {
        public List<Path> CalculateBestPath(string cities)
        {
            var TSPSolver = new TravelingSalesmanProblem.God();
            ProcessInputData processInputData = new ProcessInputData();
            ProcessOutputData processOutputData = new ProcessOutputData();

            List<string> listOfCitiesAsStrings = processInputData.ReadCities(cities);
            EvaluationMatrix matrices = new EvaluationMatrix(listOfCitiesAsStrings.Count);
            var listOfCities = processInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);
            matrices = processInputData.FillMatrixWithData(listOfCities, matrices);
            int[] orderOfCities = TSPSolver.SolveTSP(matrices.OptimalDistances);

            return processOutputData.FormOutputFromTSFResult(listOfCities, orderOfCities, matrices);
        }

    }
}
