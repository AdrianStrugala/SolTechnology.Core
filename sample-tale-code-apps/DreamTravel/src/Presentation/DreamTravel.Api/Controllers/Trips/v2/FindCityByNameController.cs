using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using DreamTravel.Domain.Cities;
using DreamTravel.Queries.FindCityByName;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.API.Filters;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.Trips.v2
{
    [ApiVersion("2.0")]
    [Route(Route)]
    [ServiceFilter(typeof(ExceptionFilter))]
    [ServiceFilter(typeof(ResponseEnvelopeFilter))]
    public class FindCityByNameController(
        IMediator mediator,
        ILogger<FindCityByNameController> logger)
        : ControllerBase
    {
        public const string Route = "api/FindCityByName";


        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<City>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Result), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> FindCityByName([FromBody] FindCityByNameQuery query)
        {
            logger.LogInformation("Looking for city: " + query.Name);
            return Ok(await mediator.Send(query));
        }
    }
}
