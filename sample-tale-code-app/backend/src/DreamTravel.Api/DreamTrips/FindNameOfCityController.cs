using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using DreamTravel.Infrastructure;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.FindNameOfCity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamTrips
{
    [Route(Route)]
    public class FindNameOfCityController : Controller
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
            var validationResult = query.Validate(new ValidationContext(query));
            if (validationResult.Any())
            {
                return BadRequest(validationResult);
            }

            try
            {
                _logger.LogInformation("Looking for city: " + query.Lat + ";" + query.Lng);

                var result = await _findNameOfCity.Handle(query);

                return Ok(result);
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
