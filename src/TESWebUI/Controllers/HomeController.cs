using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TESWebUI.Models;

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
            List<string> listOfCitiesAsStrings = ProcessInputData.ReadCities(cities);
            DistanceMatrixEvaluated matrixEvaluated = new DistanceMatrixEvaluated(listOfCitiesAsStrings.Count);
            List<City> listOfCities = ProcessInputData.GetCitiesFromGoogleApi(listOfCitiesAsStrings);

            matrixEvaluated.DownloadData(listOfCities);
            int[] result = God.SolveTSP(matrixEvaluated);

            List<Path> paths = new List<Path>();
            for (int i = 0; i < result.Length - 1; i++)
            {
                Path currentPath = new Path
                {
                    StartingCity = listOfCities[i],
                    EndingCity = listOfCities[i + 1],
                    Cost = matrixEvaluated.Costs[(i + 1) + i * listOfCitiesAsStrings.Count],
                    Distance = matrixEvaluated.Distances[(i + 1) + i  * listOfCitiesAsStrings.Count],
                    Goal = matrixEvaluated.Goals[(i + 1) + i * listOfCitiesAsStrings.Count]
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
