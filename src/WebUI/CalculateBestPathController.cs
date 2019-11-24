using System;
using System.Threading.Tasks;
using DreamTravel.DreamTrips.CalculateBestPath;
using DreamTravel.DreamTrips.CalculateBestPath.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.WebUI
{
    [Route(Route)]
    public class CalculateBestPathController : Controller
    {
        public const string Route = "api/CalculateBestPath";
        private const string PathsKeyName = "_Paths";

        private readonly ICalculateBestPath _calculateBestPath;
        private readonly ILogger<CalculateBestPathController> _logger;


        public CalculateBestPathController(ICalculateBestPath calculateBestPath,
                             ILogger<CalculateBestPathController> logger)
        {
            _calculateBestPath = calculateBestPath;
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> CalculateBestPath(Query query)
        {
            try
            {
                _logger.LogInformation("TSP Engine: Fire!");
                Result result = await _calculateBestPath.Execute(query.Cities, query.OptimizePath);


                HttpContext.Session.SetString(query.SessionId + PathsKeyName, JsonConvert.SerializeObject(result.BestPaths));

                string message = JsonConvert.SerializeObject(result.BestPaths);
                return Ok(message);
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
