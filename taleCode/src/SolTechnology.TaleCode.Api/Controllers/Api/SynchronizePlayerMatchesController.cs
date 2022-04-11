using Microsoft.AspNetCore.Mvc;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.Api.Controllers.Api
{
    [ApiController]
    public class SynchronizePlayerMatchesController : ControllerBase
    {
        private readonly ICommandHandler<SynchronizePlayerMatchesCommand> _handler;

        public SynchronizePlayerMatchesController(ICommandHandler<SynchronizePlayerMatchesCommand> handler)
        {
            _handler = handler;
        }

        [HttpGet]
        [Route("api/SynchronizePlayerMatches/{playerId}")]
        public void SynchronizePlayerMatches(int playerId)
        {
            try
            {
                _handler.Handle(new SynchronizePlayerMatchesCommand(playerId));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}