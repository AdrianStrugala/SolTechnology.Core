using System;
using System.Threading.Tasks;
using DreamTravel.LocationOfCity.Interfaces;
using DreamTravel.SharedModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.LocationOfCity
{
    [Route(Route)]
    public class Controller : Microsoft.AspNetCore.Mvc.Controller
    {
        public const string Route = "api/FindLocationOfCity";

        private readonly ILogger<Controller> _logger;
        private readonly IFindLocationOfCity _findLocationOfCity;


        public Controller(IFindLocationOfCity findLocationOfCity,
                             ILogger<Controller> logger)
        {
            _findLocationOfCity = findLocationOfCity;
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> FindLocationOfCity(string name, string sessionId)
        {
            try
            {
                _logger.LogInformation("Looking for city: " + name);

                City city = await _findLocationOfCity.Execute(name);

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
    }
}
