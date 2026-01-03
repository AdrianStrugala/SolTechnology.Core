using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using DreamTravel.Queries.CalculateBestPath;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.Api.Controllers.Trips
{
    [ApiController]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("2.0")]
    [Route("api/[controller]")]
    public class CalculateBestPathController(
        IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult> handler,
        ILogger<CalculateBestPathController> logger)
        : ControllerBase
    {
        /// <summary>
        /// V1 - DEPRECATED: Zwraca tylko listę ścieżek
        /// </summary>
        [HttpPost]
        [MapToApiVersion("1.0")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(List<Path>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CalculateBestPathV1([FromBody] CalculateBestPathQuery query)
        {
            try
            {
                logger.LogInformation("TSP Engine V1 (deprecated): Fire!");
                var result = await handler.Handle(query, CancellationToken.None);
                return Ok(result.Data.BestPaths);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                return BadRequest(JsonConvert.SerializeObject(ex.Message));
            }
        }

        /// <summary>
        /// V2 - Zwraca pełny Result wrapper
        /// </summary>
        [HttpPost]
        [MapToApiVersion("2.0")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<CalculateBestPathResult>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CalculateBestPathV2([FromBody] CalculateBestPathQuery query)
        {
            logger.LogInformation("TSP Engine V2: Fire!");
            return Ok(await handler.Handle(query, CancellationToken.None));
        }
    }
}
