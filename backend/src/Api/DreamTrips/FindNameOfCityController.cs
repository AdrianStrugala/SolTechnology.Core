using System;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.DreamTrips.FindNameOfCity;
using DreamTravel.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamTrips
{
    [Route(Route)]
    public class FindNameOfCityController : Controller
    {
        public const string Route = "api/FindNameOfCity";

        private readonly ILogger<FindNameOfCityController> _logger;
        private readonly IQueryHandler<FindNameOfCityQuery, City> _findNameOfCity;


        public FindNameOfCityController(
            IQueryHandler<FindNameOfCityQuery, City> findNameOfCity,
            ILogger<FindNameOfCityController> logger)
        {
            _findNameOfCity = findNameOfCity;
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> FindNameOfCity([FromBody] FindNameOfCityQuery request)
        {
            try
            {
                _logger.LogInformation("Looking for city: " + request.Lat + ";" + request.Lng);

                var result = await _findNameOfCity.Handle(request);

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
}
