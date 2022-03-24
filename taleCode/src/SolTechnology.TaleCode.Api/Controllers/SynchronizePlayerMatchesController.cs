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
        [Route("SynchronizePlayerMatches")]
        public void SynchronizePlayerMatches()
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