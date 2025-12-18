using System.Net;
using System.Net.Mime;
using DreamTravel.Queries.CalculateBestPath;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.Api.Controllers.v1
{
    [Route(Route)]
    public class CalculateBestPathController : Controller
    {
        public const string Route = "api/CalculateBestPath";

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
        [ProducesResponseType(typeof(List<Path>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CalculateBestPath([FromBody] CalculateBestPathQuery calculateBestPathQuery)
        {
            try
            {
                _logger.LogInformation("TSP Engine: Fire!");
                var calculateBestPathResult = await _calculateBestPath.Handle(calculateBestPathQuery, CancellationToken.None);

                return Ok(calculateBestPathResult.Data.BestPaths);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }
    }
}
