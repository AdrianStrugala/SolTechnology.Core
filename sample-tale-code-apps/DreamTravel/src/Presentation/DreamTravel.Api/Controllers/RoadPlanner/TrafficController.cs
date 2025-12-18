using DreamTravel.Commands.RecalculateTraffic;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DreamTravel.Api.Controllers.RoadPlanner;

[ApiController]
[Route("api/[controller]")]
public class TrafficController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Recalculates traffic flow based on existing and newly created streets
    /// </summary>
    /// <param name="data">The streets and intersections to analyze</param>
    /// <returns>Updated traffic information for streets</returns>
    [HttpPost("recalculate")]
    public async Task<ActionResult<RecalculateTrafficCommand>> RecalculateTraffic([FromBody] RecalculateTrafficCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }
}