using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Models;
using DreamTravel.TSPControllerHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DreamTravel.Controllers
{
    public class TSPController : Controller
    {
        private readonly IBestPathCalculator _bestPathCalculator;

        public TSPController(IBestPathCalculator bestPathCalculator)
        {
            _bestPathCalculator = bestPathCalculator;
        }

        private const string PathsKeyName = "_Paths";

        [HttpPost]
        public async Task<IActionResult> CalculateBestPath(string cities, string sessionId)
        {
            try
            {
                List<Path> paths = await _bestPathCalculator.Handle(cities);

                HttpContext.Session.SetString(sessionId + PathsKeyName, JsonConvert.SerializeObject(paths));

                string message = JsonConvert.SerializeObject(paths);
                return Ok(message);
            }

            catch (Exception ex)
            {
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> LimitCost(int costLimit, string sessionId)
        {
            try
            {
                List<Path> paths = JsonConvert.DeserializeObject<List<Path>>(HttpContext.Session.GetString(sessionId + PathsKeyName));
                var costLimitBreaker = new CostLimitBreaker();

                paths = costLimitBreaker.Handle(costLimit, paths);

                HttpContext.Session.SetString(sessionId + PathsKeyName, JsonConvert.SerializeObject(paths));

                string message = JsonConvert.SerializeObject(paths);
                return Ok(message);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
