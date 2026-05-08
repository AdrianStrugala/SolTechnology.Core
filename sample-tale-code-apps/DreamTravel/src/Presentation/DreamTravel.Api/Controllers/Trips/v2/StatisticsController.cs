﻿using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using DreamTravel.Queries.GetSearchStatistics;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DreamTravel.Api.Controllers.Trips.v2
{
    [ApiVersion("2.0")]
    [Route(Route)]
    public class StatisticsController(IMediator mediator) : ControllerBase
    {
        public const string Route = "api/statistics";

        [HttpGet ("countries")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(GetSearchStatisticsResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetSearchStatistics()
        {
            return Ok(await mediator.Send(new GetSearchStatisticsQuery()));
        }
    }
}
