﻿using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.FindCityByName;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.v1
{
    [Route(Route)]
    public class FindLocationOfCityController : Controller
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
            try
            {
                _logger.LogInformation("Looking for city: " + query.Name);

                var city = await _findLocationOfCity.Handle(query, CancellationToken.None);

                return Ok(city.Data);
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
