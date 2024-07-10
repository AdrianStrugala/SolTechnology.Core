using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.BackgroundWorker.InternalApi;

[ApiController]
public class SynchronizePlayerMatchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SynchronizePlayerMatchesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("api/SynchronizePlayerMatches/{playerId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> SynchronizePlayerMatches(int playerId) =>
        Ok(await _mediator.Send(new SynchronizePlayerMatchesCommand(playerId)));
}