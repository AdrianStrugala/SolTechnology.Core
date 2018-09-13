using System;
using System.Collections.Generic;
using DreamTravel.BestPath.Interfaces;
using DreamTravel.SharedModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.BestPath
{
    [Route(Route)]
    public class Controller : Microsoft.AspNetCore.Mvc.Controller
    {
        public const string Route = "api/CalculateBestPath";
        private const string PathsKeyName = "_Paths";

        private readonly ICalculateBestPath _calculateBestPath;
        private readonly ILogger<Controller> _logger;


        public Controller(ICalculateBestPath calculateBestPath,
                             ILogger<Controller> logger)
        {
            _calculateBestPath = calculateBestPath;
            _logger = logger;
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
    }
}
