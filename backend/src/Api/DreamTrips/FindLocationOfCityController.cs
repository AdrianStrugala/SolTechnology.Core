using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.DreamTrips.FindLocationOfCity;
using DreamTravel.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamTrips
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
            var validationResult = query.Validate(new ValidationContext(query));
            if (validationResult.Any())
            {
                return BadRequest(validationResult);
            }

            try
            {
                _logger.LogInformation("Looking for city: " + query.Name);

                City city = await _findLocationOfCity.Handle(query);

                return Ok(city);
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
