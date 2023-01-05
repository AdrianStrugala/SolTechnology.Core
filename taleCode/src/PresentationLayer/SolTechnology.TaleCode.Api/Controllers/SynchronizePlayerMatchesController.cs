using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.Api;
using SolTechnology.Core.CQRS;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.BackgroundWorker.InternalApi;

[ApiController]
public class SynchronizePlayerMatchesController : BaseController
{
    private readonly ICommandHandler<SynchronizePlayerMatchesCommand> _handler;

    public SynchronizePlayerMatchesController(ICommandHandler<SynchronizePlayerMatchesCommand> handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("api/SynchronizePlayerMatches/{playerId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async void SynchronizePlayerMatches(int playerId) =>
        // await Invoke(_handler.Handle(new SynchronizePlayerMatchesCommand(playerId)));
        await _handler.Handle(new SynchronizePlayerMatchesCommand(playerId));
}