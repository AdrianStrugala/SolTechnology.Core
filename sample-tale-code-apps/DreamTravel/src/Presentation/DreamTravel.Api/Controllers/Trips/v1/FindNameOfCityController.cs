using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using DreamTravel.Domain.Cities;
using DreamTravel.Queries.FindCityByCoordinates;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.v1
{
    [ApiVersion("1.0", Deprecated = true)]
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
            try
            {
                _logger.LogInformation("Looking for city: " + query.Lat + ";" + query.Lng);

                var result = await _findNameOfCity.Handle(query, CancellationToken.None);

                return Ok(result.Data);
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
