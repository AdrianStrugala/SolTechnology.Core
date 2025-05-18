using DreamTravel.Trips.Commands.RecalculateTraffic;
using DreamTravel.Trips.Domain.StreetGraph;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DreamTravel.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrafficController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TrafficController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Recalculates traffic flow based on existing and newly created streets
        /// </summary>
        /// <param name="data">The streets and intersections to analyze</param>
        /// <returns>Updated traffic information for streets</returns>
        [HttpPost("recalculate")]
        public async Task<ActionResult<RecalculateTrafficResult>> RecalculateTraffic([FromBody] RecalculateTrafficRequest data)
        {
            var command = new RecalculateTrafficCommand(
                data.Streets ?? new List<Street>(),
                data.Intersections ?? new List<Intersection>()
            );

            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }

    public class RecalculateTrafficRequest
    {
        public List<Street> Streets { get; set; }
        public List<Intersection> Intersections { get; set; }
    }
}
