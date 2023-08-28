using System;
using System.Collections.Generic;
using DreamTravel.Domain.Airports;
using DreamTravel.DreamFlights.GetAirports;
using DreamTravel.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamFlights
{

    [Route(Route)]
    public class GetAirportsController : Controller
    {
        public const string Route = "api/airports";

        private readonly IQueryHandler<GetAirportsQuery, List<Airport>> _getAirports;

        public GetAirportsController(IQueryHandler<GetAirportsQuery, List<Airport>> getAirports)
        {
            _getAirports = getAirports;
        }


        [HttpGet]
        public IActionResult Airports()
        {
            try
            {
                var result = _getAirports.Handle(new GetAirportsQuery());

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
