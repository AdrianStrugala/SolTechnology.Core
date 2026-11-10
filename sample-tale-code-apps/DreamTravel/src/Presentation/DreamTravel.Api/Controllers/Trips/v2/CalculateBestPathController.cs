using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using DreamTravel.Queries.CalculateBestPath;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.Api.Controllers.Trips
{
    [ApiController]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("2.0")]
    [Route("api/[controller]")]
    public class CalculateBestPathController(
        IMediator mediator,
        ILogger<CalculateBestPathController> logger)
        : ControllerBase
    {
        /// <summary>
        /// V1 - DEPRECATED: returns only the list of paths
        /// </summary>
        [HttpPost]
        [MapToApiVersion("1.0")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(List<Path>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CalculateBestPathV1([FromBody] CalculateBestPathQuery query)
        {
            logger.LogInformation("TSP Engine V1 (deprecated): Fire!");
            var result = await mediator.Send(query, HttpContext.RequestAborted);
            Result<List<Path>> bestPaths = result.IsSuccess ? result.Data!.BestPaths : result.Error!;
            return Ok(bestPaths);
        }

        /// <summary>
        /// V2 - returns the best paths and the cities they connect
        /// </summary>
        [HttpPost]
        [MapToApiVersion("2.0")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(CalculateBestPathResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CalculateBestPathV2([FromBody] CalculateBestPathQuery query)
        {
            logger.LogInformation("TSP Engine V2: Fire!");
            return Ok(await mediator.Send(query, HttpContext.RequestAborted));
        }
    }
}
