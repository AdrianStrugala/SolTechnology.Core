using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using DreamTravel.Repositories;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace DreamTravel.Controllers
{
    public class TSPController : Controller
    {
        private readonly IDatabase _redisStore;
        private readonly SessionRepository _sessionRepository;
        private ProcessInputData _processInputData;

        public TSPController(IDatabase redisStore)
        {
            _redisStore = redisStore;
            _sessionRepository = new SessionRepository(_redisStore);
        }

        [HttpPost]
        public async Task<IActionResult> CalculateBestPath(string cities, string sessionId)
        {
            try
            {
                var TSPSolver = new TravelingSalesmanProblem.God();
                _processInputData = new ProcessInputData();
                ProcessOutputData processOutputData = new ProcessOutputData();               

                List<string> listOfCitiesAsStrings = _processInputData.ReadCities(cities);
                EvaluationMatrix matrices = new EvaluationMatrix(listOfCitiesAsStrings.Count);
                var listOfCities = _processInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);

                matrices = _processInputData.FillMatrixWithData(listOfCities, matrices);
                int[] orderOfCities = TSPSolver.SolveTSP(matrices.OptimalDistances);

                List<Path> paths = processOutputData.FormOutputFromTSFResult(listOfCities, orderOfCities, matrices);

                Session currentSession = _sessionRepository.WriteToSession(sessionId, matrices);
                await _sessionRepository.AddCache(currentSession);

                return Content(JsonConvert.SerializeObject(paths));
            }

            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> LimitCost(int costLimit, string sessionId, string paths, string cities)
        {
            try
            {
                var costLimitBreaker = new CostLimitBreaker();

                Session session = await _sessionRepository.GetCache(sessionId);              
                EvaluationMatrix matrices = _sessionRepository.ReadFromSession(session);
                List<Path> pathList = (List<Path>) JsonConvert.DeserializeObject(paths);
                List<string> listOfCitiesAsStrings = _processInputData.ReadCities(cities);

                List<int> orderOfCities = new List<int>();

                for (int i = 0; i < listOfCitiesAsStrings.Count-1; i++)
                {
                    orderOfCities.Add(pathList.FindIndex(p => p.StartingCity.Name.Equals(listOfCitiesAsStrings[i])));
                }
                orderOfCities.Add(listOfCitiesAsStrings.Count-1);

                pathList = costLimitBreaker.AdjustPaths(pathList, costLimit, cities);

                return Content(JsonConvert.SerializeObject("cos"));
            }

            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}
