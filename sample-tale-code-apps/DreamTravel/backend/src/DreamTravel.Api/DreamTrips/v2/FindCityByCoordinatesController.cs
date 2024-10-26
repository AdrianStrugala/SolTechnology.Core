using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.FindCityByCoordinates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Api;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.DreamTrips.v2
{
    [Route(Route)]
    public class FindCityByCoordinatesController : ControllerBase
    {
        public const string Route = "api/v2/FindCityByCoordinates";

        private readonly ILogger<FindCityByCoordinatesController> _logger;
        private readonly IQueryHandler<FindCityByCoordinatesQuery, City> _findNameOfCity;


        public FindCityByCoordinatesController(
            IQueryHandler<FindCityByCoordinatesQuery, City> findNameOfCity,
            ILogger<FindCityByCoordinatesController> logger)
        {
            _findNameOfCity = findNameOfCity;
            _logger = logger;
        }


        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<City>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Result), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> FindCityByCoordinates([FromBody] FindCityByCoordinatesQuery query)
        {
            _logger.LogInformation("Looking for city: " + query.Lat + ";" + query.Lng);
            return Ok(await _findNameOfCity.Handle(query));
        }
    }
}
