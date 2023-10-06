using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.CalculateBestPath;
using DreamTravel.Trips.Queries.FindNameOfCity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SolTechnology.Core.Api;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.DreamTrips
{
    [Route(Route)]
    public class FindNameOfCityController : BaseController
    {
        public const string Route = "api/FindNameOfCity";

        private readonly ILogger<FindNameOfCityController> _logger;
        private readonly IQueryHandler<FindCityByCoordinatesQuery, City> _findNameOfCity;


        public FindNameOfCityController(
            IQueryHandler<FindCityByCoordinatesQuery, City> findNameOfCity,
            ILogger<FindNameOfCityController> logger)
        {
            _findNameOfCity = findNameOfCity;
            _logger = logger;
        }


        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(City), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(List<ValidationResult>), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> FindNameOfCity([FromBody] FindCityByCoordinatesQuery query)
        {
            _logger.LogInformation("TSP Engine: Fire!");
            return await Return(_findNameOfCity.Handle(query));
        }
    }
}
