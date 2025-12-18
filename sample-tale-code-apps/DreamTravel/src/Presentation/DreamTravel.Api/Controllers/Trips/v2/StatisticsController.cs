using System.Net;
using System.Net.Mime;
using DreamTravel.Queries.GetSearchStatistics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.Trips.v2
{
    [Route(Route)]
    public class StatisticsController(IMediator mediator) : ControllerBase
    {
        public const string Route = "api/v2/statistics";
        
        [HttpGet ("countries")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<GetSearchStatisticsResult>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Result), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetSearchStatistics()
        {
            return Ok(await mediator.Send(new GetSearchStatisticsQuery()));
        }
    }
}
