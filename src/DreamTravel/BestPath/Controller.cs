using DreamTravel.BestPath.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;

namespace DreamTravel.BestPath
{
    using SharedModels;
    using System.Collections.Generic;

    [Route(Route)]
    public class Controller : Microsoft.AspNetCore.Mvc.Controller
    {
        public const string Route = "api/CalculateBestPath";
        private const string PathsKeyName = "_Paths";
        private const string CitiesKeyName = "_Cities";
        private const string AllPathsKeyName = "_AllPaths";

        private readonly ICalculateBestPath _calculateBestPath;
        private readonly ILogger<Controller> _logger;


        public Controller(ICalculateBestPath calculateBestPath,
                             ILogger<Controller> logger)
        {
            _calculateBestPath = calculateBestPath;
            _logger = logger;
        }


        [HttpPost]
        public IActionResult CalculateBestPath(Query query)
        {
            try
            {
                _logger.LogInformation("TSP Engine: Fire!");
                Command command = new Command
                {
                    Cities = query.Cities,
                    OptimizePath = query.OptimizePath
                };

                try
                {
                    command.KnownCities = JsonConvert.DeserializeObject<List<City>>(HttpContext.Session.GetString(query.SessionId + CitiesKeyName));
                    command.KnownPaths = JsonConvert.DeserializeObject<List<Path>>(HttpContext.Session.GetString(query.SessionId + AllPathsKeyName));
                }
                catch (Exception)
                {
                    _logger.LogInformation($"No cities saved for session: {query.SessionId}");
                }


                Result result = _calculateBestPath.Execute(command);

                HttpContext.Session.SetString(query.SessionId + PathsKeyName, JsonConvert.SerializeObject(result.BestPaths));
                HttpContext.Session.SetString(query.SessionId + CitiesKeyName, JsonConvert.SerializeObject(result.Cities));
                HttpContext.Session.SetString(query.SessionId + AllPathsKeyName, JsonConvert.SerializeObject(result.AllPaths));

                string message = JsonConvert.SerializeObject(result.BestPaths);
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
