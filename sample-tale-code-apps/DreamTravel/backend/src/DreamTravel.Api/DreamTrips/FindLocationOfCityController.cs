using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.FindLocationOfCity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Api;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.DreamTrips
{
    [Route(Route)]
    public class FindLocationOfCityController : BaseController
    {
        public const string Route = "api/FindLocationOfCity";

        private readonly ILogger<FindLocationOfCityController> _logger;
        private readonly IQueryHandler<FindCityByNameQuery, City> _findLocationOfCity;


        public FindLocationOfCityController(
            IQueryHandler<FindCityByNameQuery, City> findLocationOfCity,
            ILogger<FindLocationOfCityController> logger)
        {
            _findLocationOfCity = findLocationOfCity;
            _logger = logger;
        }


        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(City), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(List<ValidationResult>), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> FindLocationOfCity([FromBody] FindCityByNameQuery query)
        {
            _logger.LogInformation("Looking for city: " + query.Name);
            return await Return(_findLocationOfCity.Handle(query));
        }
    }
}
