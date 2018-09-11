using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.Models;
using DreamTravel.TSPControllerHandlers.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Controllers
{
    public class TSPController : Controller
    {
        private readonly ICalculateBestPath _calculateBestPath;
        private readonly IDownloadLocationOfCity _downloadLocationOfCity;
        private readonly IBreakCostLimit _breakCostLimit;
        private readonly IDownloadCityNameByLocation _downloadCityNameByLocation;
        private readonly ILogger<TSPController> _logger;

        public TSPController(ICalculateBestPath calculateBestPath,
                             IDownloadLocationOfCity downloadLocationOfCity,
                             IDownloadCityNameByLocation downloadCityNameByLocation,
                             IBreakCostLimit breakCostLimit,
                             ILogger<TSPController> logger)
        {
            _calculateBestPath = calculateBestPath;
            _downloadLocationOfCity = downloadLocationOfCity;
            _breakCostLimit = breakCostLimit;
            _downloadCityNameByLocation = downloadCityNameByLocation;
            _logger = logger;
        }

        private const string PathsKeyName = "_Paths";

        [HttpPost]
        public async Task<IActionResult> FindCity(string name, string sessionId)
        {
            try
            {
                _logger.LogInformation("Looking for city: " + name);

                City city = await _downloadLocationOfCity.Execute(name);

                string message = JsonConvert.SerializeObject(city);
                return Ok(message);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> FindCityByLocation(double lat, double lng, string sessionId)
        {
            try
            {
                _logger.LogInformation("Looking for city: " + lat + ";" + lng);
                City result = new City
                {
                    Latitude = lat,
                    Longitude = lng
                };

                result = await _downloadCityNameByLocation.Execute(result);

                string message = JsonConvert.SerializeObject(result);
                return Ok(message);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }

        [HttpPost]
        public IActionResult CalculateBestPath(List<City> cities, string sessionId)
        {
            try
            {
                _logger.LogInformation("TSP Engine: Fire!");
                List<Path> paths = _calculateBestPath.Execute(cities);

                HttpContext.Session.SetString(sessionId + PathsKeyName, JsonConvert.SerializeObject(paths));

                string message = JsonConvert.SerializeObject(paths);
                return Ok(message);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }

        [HttpPost]
        public IActionResult LimitCost(int costLimit, string sessionId)
        {
            try
            {
                _logger.LogInformation("Limit Cost Engine: Fire!");
                List<Path> paths = JsonConvert.DeserializeObject<List<Path>>(HttpContext.Session.GetString(sessionId + PathsKeyName));

                paths = _breakCostLimit.Execute(costLimit, paths);

                HttpContext.Session.SetString(sessionId + PathsKeyName, JsonConvert.SerializeObject(paths));

                string message = JsonConvert.SerializeObject(paths);
                return Ok(message);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }
    }
}
