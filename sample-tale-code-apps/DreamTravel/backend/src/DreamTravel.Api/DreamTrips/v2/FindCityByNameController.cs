using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.FindCityByName;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Api;
using SolTechnology.Core.Api.Filters;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.DreamTrips.v2
{
    [Route(Route)]
    [ServiceFilter(typeof(LoggingFilter))]
    [ServiceFilter(typeof(ExceptionFilter))]
    [ServiceFilter(typeof(ResponseEnvelopeFilter))]
    public class FindCityByNameController : ControllerBase
    {
        public const string Route = "api/v2/FindCityByName";

        private readonly ILogger<FindCityByNameController> _logger;
        private readonly IQueryHandler<FindCityByNameQuery, City> _findLocationOfCity;


        public FindCityByNameController(
            IQueryHandler<FindCityByNameQuery, City> findLocationOfCity,
            ILogger<FindCityByNameController> logger)
        {
            _findLocationOfCity = findLocationOfCity;
            _logger = logger;
        }


        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(City), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(List<ValidationResult>), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> FindCityByName([FromBody] FindCityByNameQuery query)
        {
            _logger.LogInformation("Looking for city: " + query.Name);
            return Ok(await _findLocationOfCity.Handle(query));
        }
    }
}
