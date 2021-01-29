using System;
using System.Threading.Tasks;
using DreamTravel.DreamTrips.FindNameOfCity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamTrips
{
    [Route(Route)]
    public class FindNameOfCityController : Controller
    {
        public const string Route = "api/FindNameOfCityHandler";

        private readonly ILogger<FindNameOfCityController> _logger;
        private readonly IFindNameOfCity _findNameOfCity;


        public FindNameOfCityController(IFindNameOfCity findNameOfCity,
                          ILogger<FindNameOfCityController> logger)
        {
            _findNameOfCity = findNameOfCity;
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> FindNameOfCity([FromBody] Request request)
        {
            try
            {
                _logger.LogInformation("Looking for city: " + request.Lat + ";" + request.Lng);

                var result = await _findNameOfCity.Handle(request.Lat, request.Lng);

                return Ok(result);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }

    }

    public class Request
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string SessionId { get; set; }
    }
}
