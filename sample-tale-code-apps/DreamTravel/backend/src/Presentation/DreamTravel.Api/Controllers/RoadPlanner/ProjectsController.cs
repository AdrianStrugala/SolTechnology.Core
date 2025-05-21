using DreamTravel.GraphDatabase.Repositories;
using DreamTravel.Trips.Domain.StreetGraph;
using Microsoft.AspNetCore.Mvc;

namespace DreamTravel.Api.Controllers.RoadPlanner;

[ApiController]
[Route("api/projects/")]
public class ProjectsController(
    IIntersectionRepository intersections,
    IStreetRepository roads)
    : ControllerBase
{
    // GET api/projects/{projectId}/nodes
    [HttpGet("nodes")]
    public async Task<ActionResult<IEnumerable<Intersection>>> GetNodes()
    {
        var nodes = await intersections.GetAllAsync();
        return Ok(nodes);
    }

    // GET api/projects/{projectId}/streets
    [HttpGet("streets")]
    public async Task<ActionResult<IEnumerable<Street>>> GetRoads()
    {
        var roads1 = await roads.GetAllAsync();
        return Ok(roads1);
    }
}