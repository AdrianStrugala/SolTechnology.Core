using Microsoft.AspNetCore.Mvc;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;
using SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;

namespace SolTechnology.TaleCode.Api.Controllers
{
    [ApiController]
    public class GetPlayerStatisticsController : ControllerBase
    {
        private readonly IQueryHandler<GetPlayerStatisticsQuery, GetPlayerStatisticsResult> _handler;

        public GetPlayerStatisticsController(IQueryHandler<GetPlayerStatisticsQuery, GetPlayerStatisticsResult> handler)
        {
            _handler = handler;
        }

        [HttpGet]
        [Route("GetPlayerStatistics/{playerId}")]
        public async Task<GetPlayerStatisticsResult> GetPlayerStatistics(int playerId)
        {
            try
            {
                return await _handler.Handle(new GetPlayerStatisticsQuery(playerId));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}