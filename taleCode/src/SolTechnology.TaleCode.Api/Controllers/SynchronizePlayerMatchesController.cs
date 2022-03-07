using Microsoft.AspNetCore.Mvc;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.Api.Controllers
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
        [Route("SynchronizePlayerMatches/{playerId}")]
        public void SynchronizePlayerMatches(int playerId)
        {
            try
            {
                _handler.Handle(new SynchronizePlayerMatchesCommand("Cristiano Ronaldo"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}