using DreamTravel.BestPath.Interfaces;
using DreamTravel.SharedModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        public IActionResult CalculateBestPath([FromBody]Query query)
        {
            try
            {
                _logger.LogInformation("TSP Engine: Fire!");

                Command command = new Command
                {
                    Cities = query.Cities,
                    OptimizePath = query.OptimizePath
                };                
                List<Path> bestPaths = _calculateBestPath.Execute(command).BestPaths;

                HttpContext.Session.SetString(query.SessionId + PathsKeyName, JsonConvert.SerializeObject(bestPaths));

                string message = JsonConvert.SerializeObject(bestPaths);
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
