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

        public async Task<List<Path>> Handle(List<City> cities)
        {         
            EvaluationMatrix matrices = new EvaluationMatrix(cities.Count);
            matrices = _processInputData.DownloadExternalData(cities, matrices);
            matrices = _evaluationBrain.EvaluateCost(matrices, cities.Count);          
            int[] orderOfCities = _tspSolver.SolveTSP(matrices.OptimalDistances);

            //to have a possiblity to store cities data
           // File.WriteAllText("./twentyCities.txt", JsonConvert.SerializeObject(matrices.OptimalDistances));

            return _processOutputData.FormOutputFromTSPResult(cities, orderOfCities, matrices);
        }

    }
}
