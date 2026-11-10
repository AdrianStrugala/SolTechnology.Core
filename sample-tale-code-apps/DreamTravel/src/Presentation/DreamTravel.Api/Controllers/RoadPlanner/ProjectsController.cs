using System.Net;
using DreamTravel.Domain.StreetGraph;
using DreamTravel.Queries.GetIntersections;
using DreamTravel.Queries.GetStreets;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.RoadPlanner;

[ApiController]
[Route("api/projects/")]
public class ProjectsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns every intersection of the street graph
    /// </summary>
    [HttpGet("nodes")]
    [ProducesResponseType(typeof(List<Intersection>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetNodes()
    {
        return Ok(await mediator.Send(new GetIntersectionsQuery(), HttpContext.RequestAborted));
    }

    /// <summary>
    /// Returns every street of the street graph
    /// </summary>
    [HttpGet("streets")]
    [ProducesResponseType(typeof(List<Street>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetRoads()
    {
        return Ok(await mediator.Send(new GetStreetsQuery(), HttpContext.RequestAborted));
    }
}
