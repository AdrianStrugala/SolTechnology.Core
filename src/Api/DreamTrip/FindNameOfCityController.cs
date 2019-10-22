using System;
using System.Threading.Tasks;
using DreamTravel.Features.DreamTrip.FindNameOfCity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamTrip
{
    [Route(Route)]
    public class FindNameOfCityController : Controller
    {
        public const string Route = "api/FindNameOfCity";

        private readonly ILogger<FindNameOfCityController> _logger;
        private readonly IFindNameOfCity _findNameOfCity;


        public FindNameOfCityController(IFindNameOfCity findNameOfCity,
                          ILogger<FindNameOfCityController> logger)
        {
            _findNameOfCity = findNameOfCity;
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> FindNameOfCity(double lat, double lng, string sessionId)
        {
            try
            {
                _logger.LogInformation("Looking for city: " + lat + ";" + lng);

                var result = await _findNameOfCity.Execute(lat, lng);

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
    }
}
