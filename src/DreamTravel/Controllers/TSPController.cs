using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DreamTravel.Controllers
{
    public class TSPController : Controller
    {
        private ProcessInputData _processInputData;

        private const string PathsKeyName = "_Paths";

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

                HttpContext.Session.SetString(sessionId + PathsKeyName, JsonConvert.SerializeObject(paths));

                // return Ok();
                return Content(JsonConvert.SerializeObject(paths));
            }

            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> LimitCost(int costLimit, string sessionId)
        {
            try
            {
                List<Path> paths = JsonConvert.DeserializeObject<List<Path>>(HttpContext.Session.GetString(sessionId + PathsKeyName));
                var costLimitBreaker = new CostLimitBreaker();

                paths = costLimitBreaker.AdjustPaths(costLimit, paths);

                HttpContext.Session.SetString(sessionId + PathsKeyName, JsonConvert.SerializeObject(paths));
                // return Ok();
                return Content(JsonConvert.SerializeObject(paths));
            }

            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}
