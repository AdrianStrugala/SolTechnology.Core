using System;
using System.Collections.Generic;
using DreamTravel.Domain.Paths;
using DreamTravel.Features.DreamTrip.LimitCostOfPaths;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.WebUI
{
    [Route(Route)]
    public class LimitCostOfPathsController : Controller
    {
        public const string Route = "api/LimitCost";

        private readonly ILimitCostOfPaths _breakCostLimit;
        private readonly ILogger<Controller> _logger;

        public LimitCostOfPathsController(ILimitCostOfPaths breakCostLimit,
                          ILogger<Controller> logger)
        {
            _breakCostLimit = breakCostLimit;
            _logger = logger;
        }

        private const string PathsKeyName = "_Paths";



        [HttpPost]
        public IActionResult LimitCost(int costLimit, string sessionId)
        {
            try
            {
                _logger.LogInformation("Limit Cost Engine: Fire!");
                List<Path> paths = JsonConvert.DeserializeObject<List<Path>>(HttpContext.Session.GetString(sessionId + PathsKeyName));

                paths = _breakCostLimit.Execute(costLimit, paths);

                HttpContext.Session.SetString(sessionId + PathsKeyName, JsonConvert.SerializeObject(paths));

                string message = JsonConvert.SerializeObject(paths);
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
