using System;
using DreamTravel.DreamFlights.GetAirports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamFlights
{

    [Route(Route)]
    public class GetAirportsController : Controller
    {
        public const string Route = "api/airports";

        private readonly IGetAirports _getAirports;

        public GetAirportsController(IGetAirports getAirports)
        {
            _getAirports = getAirports;
        }


        [HttpGet]
        public IActionResult Airports()
        {
            try
            {
                var result = _getAirports.Execute();

                return Ok(result);
            }

            catch (Exception ex)
            {
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }
    }
}
