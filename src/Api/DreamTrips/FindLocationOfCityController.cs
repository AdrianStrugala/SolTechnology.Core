using System;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.DreamTrips.FindLocationOfCity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamTrips
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
        public async Task<IActionResult> FindLocationOfCity([FromBody]FindLocationOfCityQuery query)
        {
            try
            {
                _logger.LogInformation("Looking for city: " + query.Name);

                City city = await _findLocationOfCity.Handle(query);

                return Ok(city);
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
