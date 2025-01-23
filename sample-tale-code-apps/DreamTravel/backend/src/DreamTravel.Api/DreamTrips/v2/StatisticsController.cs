using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.FindCityByCoordinates;
using DreamTravel.Trips.Queries.GetSearchStatistics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Api;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.DreamTrips.v2
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
