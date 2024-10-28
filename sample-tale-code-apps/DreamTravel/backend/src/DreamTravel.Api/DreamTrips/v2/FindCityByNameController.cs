using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.FindCityByName;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Api.Filters;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.DreamTrips.v2
{
    [Route(Route)]
    [ServiceFilter(typeof(ExceptionFilter))]
    [ServiceFilter(typeof(ResponseEnvelopeFilter))]
    public class FindCityByNameController(
        IMediator mediator,
        ILogger<FindCityByNameController> logger)
        : ControllerBase
    {
        public const string Route = "api/v2/FindCityByName";


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
