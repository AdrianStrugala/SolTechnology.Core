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

            matrixEvaluated.FillWithData(listOfCities);
            int[] result = God.SolveTSP(matrixEvaluated);

            string resultAsString = null;

            for (int i = 0; i < result.Length - 1; i++)
            {
                resultAsString += listOfCities[result[i]].Name + ";";
            }

            resultAsString += listOfCities[result[result.Length - 1]].Name;

            return Content(resultAsString);
        }


        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
