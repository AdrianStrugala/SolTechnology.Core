using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using DreamTravel.Domain.Cities;
using DreamTravel.Queries.FindCityByCoordinates;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.v1
{
    [ApiVersion("1.0", Deprecated = true)]
    [Route(Route)]
    public class FindNameOfCityController(
        IMediator mediator,
        ILogger<FindNameOfCityController> logger)
        : Controller
    {
        public const string Route = "api/FindNameOfCity";

        /// <summary>
        /// V1 - DEPRECATED: finds a city's name by its coordinates
        /// </summary>
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(City), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> FindNameOfCity([FromBody] FindCityByCoordinatesQuery query)
        {
            logger.LogInformation("Looking for city: " + query.Lat + ";" + query.Lng);
            return Ok(await mediator.Send(query, HttpContext.RequestAborted));
        }
    }
}
