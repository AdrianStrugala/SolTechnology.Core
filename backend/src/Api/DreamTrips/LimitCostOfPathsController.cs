using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Paths;
using DreamTravel.DreamTrips.LimitCostOfPaths;
using DreamTravel.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamTrips
{
    [Route(Route)]
    public class LimitCostOfPathsController : Controller
    {
        public const string Route = "api/LimitCost";

        private readonly IQueryHandler<LimitCostOfPathsInput, List<Path>> _limitCostsOfPathsHandler;
        private readonly ILogger<Controller> _logger;

        public LimitCostOfPathsController(IQueryHandler<LimitCostOfPathsInput, List<Path>> _limitCostsOfPathsHandler,
                          ILogger<Controller> logger)
        {
            this._limitCostsOfPathsHandler = _limitCostsOfPathsHandler;
            _logger = logger;
        }

        private const string PathsKeyName = "_Paths";



        [HttpPost]
        public async Task<IActionResult> LimitCost(int costLimit, string sessionId)
        {
            try
            {
                _logger.LogInformation("Limit Cost Engine: Fire!");
                List<Path> paths = JsonConvert.DeserializeObject<List<Path>>(HttpContext.Session.GetString(sessionId + PathsKeyName));

                paths = await _limitCostsOfPathsHandler.Handle(new LimitCostOfPathsInput
                {
                    CostLimit = costLimit,
                    Paths = paths
                });

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
