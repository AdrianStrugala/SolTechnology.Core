using System.Net;
using System.Net.Mime;
using DreamTravel.Trips.Queries.LimitCostOfPaths;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Api.Controllers.v1
{
    [Route(Route)]
    [Obsolete]
    public class LimitCostOfPathsController : Controller
    {
        public const string Route = "api/LimitCost";

        private readonly IQueryHandler<LimitCostOfPathsQuery, List<Path>> _limitCostsOfPathsHandler;
        private readonly ILogger<Controller> _logger;

        public LimitCostOfPathsController(
            IQueryHandler<LimitCostOfPathsQuery, List<Path>> _limitCostsOfPathsHandler,
            ILogger<Controller> logger)
        {
            this._limitCostsOfPathsHandler = _limitCostsOfPathsHandler;
            _logger = logger;
        }

        private const string PathsKeyName = "_Paths";



        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(List<Path>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> LimitCost(int costLimit, string sessionId)
        {
            try
            {
                _logger.LogInformation("Limit Cost Engine: Fire!");
                List<Path> paths = JsonConvert.DeserializeObject<List<Path>>(HttpContext.Session.GetString(sessionId + PathsKeyName));

                paths = (await _limitCostsOfPathsHandler.Handle(new LimitCostOfPathsQuery
                {
                    CostLimit = costLimit,
                    Paths = paths
                }, CancellationToken.None)).Data;

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
