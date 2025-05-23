﻿using System.Net;
using System.Net.Mime;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.FindCityByCoordinates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.v2
{
    [Route(Route)]
    public class FindCityByCoordinatesController(
        IMediator mediator,
        ILogger<FindCityByCoordinatesController> logger)
        : ControllerBase
    {
        public const string Route = "api/v2/FindCityByCoordinates";


        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<City>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Result), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> FindCityByCoordinates([FromBody] FindCityByCoordinatesQuery query)
        {
            logger.LogInformation("Looking for city: " + query.Lat + ";" + query.Lng);
            return Ok(await mediator.Send(query));
        }
    }
}
