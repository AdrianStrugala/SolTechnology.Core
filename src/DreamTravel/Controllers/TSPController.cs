using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace DreamTravel.Controllers
{
    public class TSPController : Controller
    {
        private readonly IDatabase _redisStore;

        public TSPController(IDatabase redisStore)
        {
            _redisStore = redisStore;
        }

        [HttpPost]
        public async Task<IActionResult> CalculateBestPath(string cities, int sessionId)
        {
            var TSPSolver = new TravelingSalesmanProblem.God();
            ProcessInputData processInputData = new ProcessInputData();
            ProcessOutputData processOutputData = new ProcessOutputData();
            SessionRepository sessionRepository = new SessionRepository(_redisStore);

            List<string> listOfCitiesAsStrings = processInputData.ReadCities(cities);
            EvaluationMatrix matrices = new EvaluationMatrix(listOfCitiesAsStrings.Count);
            var listOfCities = processInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);

            matrices = processInputData.FillMatrixWithData(listOfCities, matrices);
            int[] orderOfCities = TSPSolver.SolveTSP(matrices.OptimalDistances);

            List<Path> paths = processOutputData.FormOutputFromTSFResult(listOfCities, orderOfCities, matrices);

            Session currentSession = sessionRepository.CreateSesstion(sessionId, matrices);
            await sessionRepository.AddCache(currentSession);

            return Content(JsonConvert.SerializeObject(paths));
        }
    }
}
