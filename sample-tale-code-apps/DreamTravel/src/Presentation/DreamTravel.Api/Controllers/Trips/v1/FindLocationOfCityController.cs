using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using DreamTravel.Domain.Cities;
using DreamTravel.Queries.FindCityByName;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.v1
{
    [ApiVersion("1.0", Deprecated = true)]
    [Route(Route)]
    public class FindLocationOfCityController(
        IQueryHandler<FindCityByNameQuery, City> findLocationOfCity,
        ILogger<FindLocationOfCityController> logger)
        : Controller
    {
        public const string Route = "api/FindLocationOfCity";


        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(City), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(List<ValidationResult>), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> FindLocationOfCity([FromBody] FindCityByNameQuery query)
        {
            try
            {
                logger.LogInformation("Looking for city: " + query.Name);

                var city = await findLocationOfCity.Handle(query, CancellationToken.None);

                return Ok(city.Data);
            }

            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }
    }
}
