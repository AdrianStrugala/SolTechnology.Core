using System;
using System.Threading.Tasks;
using DreamTravel.DreamTrips.CalculateBestPath;
using DreamTravel.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamTrips
{
    [Route(Route)]
    public class CalculateBestPathController : Controller
    {
        public const string Route = "api/CalculateBestPath";

        private readonly IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult> _calculateBestPath;
        private readonly ILogger<CalculateBestPathController> _logger;


        public CalculateBestPathController(IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult> calculateBestPath,
                             ILogger<CalculateBestPathController> logger)
        {
            _calculateBestPath = calculateBestPath;
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> CalculateBestPath([FromBody] CalculateBestPathQuery calculateBestPathQuery)
        {
            try
            {
                _logger.LogInformation("TSP Engine: Fire!");
                CalculateBestPathResult calculateBestPathResult = await _calculateBestPath.Handle(calculateBestPathQuery);

                return Ok(calculateBestPathResult.BestPaths);
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
