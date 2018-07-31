using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using Newtonsoft.Json;
using TravelingSalesmanProblem;
using Path = DreamTravel.Models.Path;

namespace DreamTravel.TSPControllerHandlers
{
    public class BestPathCalculator : IBestPathCalculator
    {
        private readonly IProcessInputData _processInputData;
        private readonly IProcessOutputData _processOutputData;
        private readonly ITSP _tspSolver;
        private readonly IEvaluationBrain _evaluationBrain;

        public BestPathCalculator(IProcessInputData processInputData, IProcessOutputData processOutputData, ITSP tspSolver, IEvaluationBrain evaluationBrain)
        {
            _processInputData = processInputData;
            _processOutputData = processOutputData;
            _tspSolver = tspSolver;
            _evaluationBrain = evaluationBrain;
        }

        public async Task<List<Path>> Handle(string cities)
        {
            List<string> listOfCitiesAsStrings = _processInputData.ReadCities(cities);
            EvaluationMatrix matrices = new EvaluationMatrix(listOfCitiesAsStrings.Count);
            var listOfCities = await _processInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);
            matrices = _processInputData.DownloadExternalData(listOfCities, matrices);
            matrices = _evaluationBrain.EvaluateCost(matrices, listOfCities.Count);          
            int[] orderOfCities = _tspSolver.SolveTSP(matrices.OptimalDistances);

            //to have a possiblity to store cities data
           // File.WriteAllText("./twentyCities.txt", JsonConvert.SerializeObject(matrices.OptimalDistances));

            return _processOutputData.FormOutputFromTSPResult(listOfCities, orderOfCities, matrices);
        }

    }
}
