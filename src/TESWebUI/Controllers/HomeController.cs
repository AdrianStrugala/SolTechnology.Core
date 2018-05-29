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
            ProcessOutputData processOutputData = new ProcessOutputData();

            List<string> listOfCitiesAsStrings = processInputData.ReadCities(cities);
            DistanceMatrixEvaluated matrixEvaluated = new DistanceMatrixEvaluated(listOfCitiesAsStrings.Count);
            var listOfCities = processInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);

            matrixEvaluated = processInputData.FillMatrixWithData(listOfCities, matrixEvaluated);
            int[] orderOfCities = TSPSolver.SolveTSP(matrixEvaluated);

            List<Path> paths = processOutputData.FormOutputFromTSFResult(listOfCities, orderOfCities, matrixEvaluated);
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(paths));
        }


        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
