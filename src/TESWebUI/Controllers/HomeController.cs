using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TESWebUI.ExternalConnection;
using TESWebUI.Models.ViewModels;
using TravelingSalesmanProblem.Models;

namespace TESWebUI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CalculateBestPath(string cities)
        {
            var TSPSolver = new TravelingSalesmanProblem.God();
            ProcessInputData processInputData = new ProcessInputData();

            List<string> listOfCitiesAsStrings = processInputData.ReadCities(cities);
            DistanceMatrixEvaluated matrixEvaluated = new DistanceMatrixEvaluated(listOfCitiesAsStrings.Count);
            var listOfCities = processInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);

            matrixEvaluated = processInputData.FillMatrixWithData(listOfCities, matrixEvaluated);
            int[] result = TSPSolver.SolveTSP(matrixEvaluated);

            List<Path> paths = new List<Path>();
            for (int i = 0; i < result.Length - 1; i++)
            {
                Path currentPath = new Path
                {
                    StartingCity = listOfCities[result[i]],
                    EndingCity = listOfCities[result[i + 1]],
                    Cost = matrixEvaluated.Costs[result[i + 1] + result[i] * listOfCitiesAsStrings.Count],
                    Distance = matrixEvaluated.Distances[result[i + 1] + result[i]  * listOfCitiesAsStrings.Count],
                    Goal = matrixEvaluated.Goals[result[i + 1] + result[i] * listOfCitiesAsStrings.Count]
                };
                paths.Add(currentPath);
            }
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(paths));
        }


        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
