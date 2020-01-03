using System;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.DreamTrips.FindLocationOfCity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.WebUI
{
    [Route(Route)]
    public class FindLocationOfCityController : Controller
    {
        public const string Route = "api/FindLocationOfCity";

        private readonly ILogger<FindLocationOfCityController> _logger;
        private readonly IFindLocationOfCity _findLocationOfCity;


        public FindLocationOfCityController(IFindLocationOfCity findLocationOfCity,
        ILogger<FindLocationOfCityController> logger)
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