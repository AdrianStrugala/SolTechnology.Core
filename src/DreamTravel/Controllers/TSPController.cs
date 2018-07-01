using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using DreamTravel.TSPControllerHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DreamTravel.Controllers
{
    public class TSPController : Controller
    {
        private const string PathsKeyName = "_Paths";

        [HttpPost]
        public async Task<IActionResult> CalculateBestPath(string cities, string sessionId)
        {
            try
            {
                BestPathCalculator bestPathCalculator = new BestPathCalculator();

                List<Path> paths = bestPathCalculator.CalculateBestPath(cities);

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
