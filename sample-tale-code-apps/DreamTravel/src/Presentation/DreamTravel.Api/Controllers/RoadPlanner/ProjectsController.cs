using DreamTravel.GraphDatabase.Repositories;
using DreamTravel.Trips.Domain.StreetGraph;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.RoadPlanner;

[ApiController]
[Route("api/projects/")]
public class ProjectsController(
    IIntersectionRepository intersections,
    IStreetRepository roads)
    : ControllerBase
{
    // GET api/projects/nodes
    [HttpGet("nodes")]
    public async Task<ActionResult<Result<List<Intersection>>>> GetNodes()
    {
        var nodes = await intersections.GetAllAsync();
        return Ok(Result<List<Intersection>>.Success(nodes.ToList()));
    }

    // GET api/projects/streets
    [HttpGet("streets")]
    public async Task<ActionResult<Result<List<Street>>>> GetRoads()
    {
        var roads1 = await roads.GetAllAsync();
        return Ok(Result<List<Street>>.Success(roads1.ToList()));
    }
}