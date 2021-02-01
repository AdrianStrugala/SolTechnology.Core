using System;
using System.Linq;
using System.Threading.Tasks;
using DreamTravel.DreamTrips.CalculateBestPath;
using DreamTravel.DreamTrips.CalculateBestPath.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamTrips
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
        public async Task<IActionResult> CalculateBestPath([FromBody]CalculateBestPathQuery calculateBestPathQuery)
        {
            try
            {
                var sanitizedCities = calculateBestPathQuery.Cities.Where(c => c != null).ToList();

                _logger.LogInformation("TSP Engine: Fire!");
                CalculateBestPathResult calculateBestPathResult = await _calculateBestPath.Execute(sanitizedCities);

                //                HttpContext.Session.SetString(calculateBestPathQuery.SessionId + PathsKeyName, JsonConvert.SerializeObject(calculateBestPathResult.BestPaths));

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
