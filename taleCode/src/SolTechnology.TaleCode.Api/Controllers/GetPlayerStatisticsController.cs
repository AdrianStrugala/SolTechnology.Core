using Microsoft.AspNetCore.Mvc;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.Api.Controllers
{
    [ApiController]
    public class GetPlayerStatisticsController : ControllerBase
    {
        private readonly ICommandHandler<SynchronizePlayerMatchesCommand> _handler;

        public GetPlayerStatisticsController(ICommandHandler<SynchronizePlayerMatchesCommand> handler)
        {
            _handler = handler;
        }

        [HttpGet]
        [Route("GetPlayerStatistics/{playerId}")]
        public void GetPlayerStatistics(int playerId)
        {
            try
            {
                _handler.Handle(new SynchronizePlayerMatchesCommand(44));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}