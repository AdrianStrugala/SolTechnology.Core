using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using DreamTravel.Domain.Cities;
using DreamTravel.Queries.FindCityByName;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.v1
{
    [ApiVersion("1.0", Deprecated = true)]
    [Route(Route)]
    public class FindLocationOfCityController(
        IMediator mediator,
        ILogger<FindLocationOfCityController> logger)
        : Controller
    {
        public const string Route = "api/FindLocationOfCity";

        /// <summary>
        /// V1 - DEPRECATED: finds a city's location by its name
        /// </summary>
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(City), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> FindLocationOfCity([FromBody] FindCityByNameQuery query)
        {
            logger.LogInformation("Looking for city: " + query.Name);
            return Ok(await mediator.Send(query, HttpContext.RequestAborted));
        }
    }
}
