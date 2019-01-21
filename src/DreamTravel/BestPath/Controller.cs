namespace DreamTravel.BestPath
{
    using Interfaces;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;

    [Route(Route)]
    public class Controller : Microsoft.AspNetCore.Mvc.Controller
    {
        public const string Route = "api/CalculateBestPath";
        private const string PathsKeyName = "_Paths";

        private readonly ICalculateBestPath _calculateBestPath;
        private readonly ILogger<Controller> _logger;


        public Controller(ICalculateBestPath calculateBestPath,
                             ILogger<Controller> logger)
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
