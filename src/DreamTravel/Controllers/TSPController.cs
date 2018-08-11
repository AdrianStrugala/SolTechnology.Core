using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.Models;
using DreamTravel.TSPControllerHandlers.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DreamTravel.Controllers
{
    public class TSPController : Controller
    {
        private readonly ICalculateBestPath _calculateBestPath;
        private readonly IDownloadLocationOfCity _downloadLocationOfCity;
        private readonly IBreakCostLimit _breakCostLimit;

        public TSPController(ICalculateBestPath calculateBestPath,
                             IDownloadLocationOfCity downloadLocationOfCity,
                             IBreakCostLimit breakCostLimit)
        {
            _calculateBestPath = calculateBestPath;
            _downloadLocationOfCity = downloadLocationOfCity;
            _breakCostLimit = breakCostLimit;
        }

        private const string PathsKeyName = "_Paths";

        [HttpPost]
        public async Task<IActionResult> FindCity(string name, string sessionId)
        {
            try
            {
                City city = await _downloadLocationOfCity.Execute(name);

                string message = JsonConvert.SerializeObject(city);
                return Ok(message);
            }

            catch (Exception ex)
            {
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CalculateBestPath(List<City> cities, string sessionId)
        {
            try
            {
                List<Path> paths = await _calculateBestPath.Execute(cities);

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
        public IActionResult LimitCost(int costLimit, string sessionId)
        {
            try
            {
                List<Path> paths = JsonConvert.DeserializeObject<List<Path>>(HttpContext.Session.GetString(sessionId + PathsKeyName));

                paths = _breakCostLimit.Execute(costLimit, paths);

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
    }
}
