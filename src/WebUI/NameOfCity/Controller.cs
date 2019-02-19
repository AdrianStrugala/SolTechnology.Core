namespace DreamTravel.WebUI.NameOfCity
{
    using System;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using SharedModels;

    [Route(Route)]
    public class Controller : Microsoft.AspNetCore.Mvc.Controller
    {
        public const string Route = "api/FindNameOfCity";

        private readonly ILogger<Controller> _logger;
        private readonly IFindNameOfCity _findNameOfCity;


        public Controller(IFindNameOfCity findNameOfCity,
                          ILogger<Controller> logger)
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
                City result = new City
                {
                    Latitude = lat,
                    Longitude = lng
                };

                result = await _findNameOfCity.Execute(result);

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
