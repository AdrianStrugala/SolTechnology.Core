using System.Collections.Generic;
using DreamTravel.ExternalConnection;
using Microsoft.AspNetCore.Mvc;
using TravelingSalesmanProblem.Models;

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
            EvaluationMatrix matrix = new EvaluationMatrix(listOfCitiesAsStrings.Count);
            var listOfCities = processInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);

            matrix = processInputData.FillMatrixWithData(listOfCities, matrix);
            int[] orderOfCities = TSPSolver.SolveTSP(matrix.Distances);

            List<Path> paths = processOutputData.FormOutputFromTSFResult(listOfCities, orderOfCities, matrix);
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(paths));
        }
    }
}
