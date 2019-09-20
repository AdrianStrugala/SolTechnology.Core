using DreamTravel.Features.FindNameOfCity.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.WebUI.NameOfCity
{
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
