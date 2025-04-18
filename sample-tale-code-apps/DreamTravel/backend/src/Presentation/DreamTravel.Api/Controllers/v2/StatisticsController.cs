using System.Net;
using System.Net.Mime;
using DreamTravel.Trips.Queries.GetSearchStatistics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.v2
{
    [Route(Route)]
    public class StatisticsController(IMediator mediator) : ControllerBase
    {
        public const string Route = "api/v2/statistics";


        [HttpPost ("countries")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<GetSearchStatisticsResult>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Result), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetSearchStatistics([FromBody] GetSearchStatisticsQuery query)
        {
            return Ok(await mediator.Send(query));
        }
    }
}
