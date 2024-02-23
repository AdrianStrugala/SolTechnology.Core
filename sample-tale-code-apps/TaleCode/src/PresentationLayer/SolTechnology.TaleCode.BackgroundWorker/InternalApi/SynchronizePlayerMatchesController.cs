using System.Net;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.BackgroundWorker.InternalApi;

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
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> SynchronizePlayerMatches(int playerId) =>
        Ok(await _handler.Handle(new SynchronizePlayerMatchesCommand(playerId)));
}