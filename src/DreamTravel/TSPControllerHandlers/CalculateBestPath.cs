using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.Models;
using DreamTravel.TSPControllerHandlers.Interfaces;
using Newtonsoft.Json;
using TravelingSalesmanProblem;
using Path = DreamTravel.Models.Path;

namespace DreamTravel.TSPControllerHandlers
{
    public class CalculateBestPath : ICalculateBestPath
    {
        private readonly IProcessInputData _processInputData;
        private readonly IFormOutputDataForBestPath _formOutputDataForBestPath;
        private readonly ITSP _tspSolver;
        private readonly IEvaluationBrain _evaluationBrain;

        public CalculateBestPath(IProcessInputData processInputData, IFormOutputDataForBestPath formOutputDataForBestPath, ITSP tspSolver, IEvaluationBrain evaluationBrain)
        {
            _processInputData = processInputData;
            _formOutputDataForBestPath = formOutputDataForBestPath;
            _tspSolver = tspSolver;
            _evaluationBrain = evaluationBrain;
        }

        public async Task<List<Path>> Execute(List<City> cities)
        {         
            EvaluationMatrix matrices = new EvaluationMatrix(cities.Count);
            matrices = _processInputData.Execute(cities, matrices);
            matrices = _evaluationBrain.Execute(matrices, cities.Count);          
            int[] orderOfCities = _tspSolver.SolveTSP(matrices.OptimalDistances);

            //to have a possiblity to store cities data
           // File.WriteAllText("./twentyCities.txt", JsonConvert.SerializeObject(matrices.OptimalDistances));

            return _formOutputDataForBestPath.Execute(cities, orderOfCities, matrices);
        }

    }
}
