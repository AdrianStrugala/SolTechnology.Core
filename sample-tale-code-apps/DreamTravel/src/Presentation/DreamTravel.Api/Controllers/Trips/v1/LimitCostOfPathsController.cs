using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using DreamTravel.Queries.LimitCostOfPaths;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.Api.Controllers.v1
{
    [ApiVersion("1.0", Deprecated = true)]
    [Route(Route)]
    [Obsolete]
    public class LimitCostOfPathsController(
        IMediator mediator,
        ILogger<LimitCostOfPathsController> logger)
        : Controller
    {
        public const string Route = "api/LimitCost";

        /// <summary>
        /// V1 - DEPRECATED: keeps the toll roads whose cost fits the limit and frees the rest
        /// </summary>
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(List<Path>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> LimitCost([FromBody] LimitCostOfPathsQuery query)
        {
            logger.LogInformation("Limit Cost Engine: Fire!");
            return Ok(await mediator.Send(query, HttpContext.RequestAborted));
        }
    }
}
