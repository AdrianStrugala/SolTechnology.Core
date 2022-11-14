using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.Api;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.Api.Controllers.Api;

[ApiController]
public class SynchronizePlayerMatchesController : BaseController
{
    private readonly ICommandHandler<SynchronizePlayerMatchesCommand> _handler;

    public SynchronizePlayerMatchesController(ICommandHandler<SynchronizePlayerMatchesCommand> handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [Route("api/SynchronizePlayerMatches/{playerId}")]
    public async void SynchronizePlayerMatches(int playerId) =>
        await Invoke(_handler.Handle(new SynchronizePlayerMatchesCommand(playerId)));
}