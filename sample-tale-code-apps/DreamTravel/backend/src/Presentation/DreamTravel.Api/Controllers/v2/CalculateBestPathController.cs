using System.Net;
using System.Net.Mime;
using DreamTravel.Trips.Queries.CalculateBestPath;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Controllers.v2
{
    [Route(Route)]
    public class CalculateBestPathController : ControllerBase
    {
        public const string Route = "api/v2/CalculateBestPath";

        private readonly IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult> _calculateBestPath;
        private readonly ILogger<CalculateBestPathController> _logger;


        public CalculateBestPathController(
            IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult> calculateBestPath,
            ILogger<CalculateBestPathController> logger)
        {
            _calculateBestPath = calculateBestPath;
            _logger = logger;
        }


        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<CalculateBestPathResult>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CalculateBestPath([FromBody] CalculateBestPathQuery calculateBestPathQuery)
        {
            _logger.LogInformation("TSP Engine: Fire!");
            return Ok(await _calculateBestPath.Handle(calculateBestPathQuery, CancellationToken.None));
        }
    }
}
