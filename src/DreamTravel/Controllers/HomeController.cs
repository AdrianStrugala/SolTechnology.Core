using System.Collections.Generic;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using Microsoft.AspNetCore.Mvc;

namespace DreamTravel.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CalculateBestPath(string cities, string sessionId)
        {
            var TSPSolver = new TravelingSalesmanProblem.God();
            ProcessInputData processInputData = new ProcessInputData();
            ProcessOutputData processOutputData = new ProcessOutputData();

            List<string> listOfCitiesAsStrings = processInputData.ReadCities(cities);
            EvaluationMatrix matrices = new EvaluationMatrix(listOfCitiesAsStrings.Count);
            var listOfCities = processInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);

            matrices = processInputData.FillMatrixWithData(listOfCities, matrices);
            int[] orderOfCities = TSPSolver.SolveTSP(matrices.OptimalDistances);

            List<Path> paths = processOutputData.FormOutputFromTSFResult(listOfCities, orderOfCities, matrices);
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(paths));
        }
    }
}
