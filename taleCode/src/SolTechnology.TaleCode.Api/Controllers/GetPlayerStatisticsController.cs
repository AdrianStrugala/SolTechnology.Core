using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.Api;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;

namespace SolTechnology.TaleCode.Api.Controllers;

[ApiController]
public class GetPlayerStatisticsController : BaseController
{
    private readonly IQueryHandler<GetPlayerStatisticsQuery, GetPlayerStatisticsResult> _handler;

    public GetPlayerStatisticsController(IQueryHandler<GetPlayerStatisticsQuery, GetPlayerStatisticsResult> handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [Route("GetPlayerStatistics/{playerId}")]
    public async Task<IActionResult> GetPlayerStatistics(int playerId) =>
        await Return(_handler.Handle(new GetPlayerStatisticsQuery(playerId)));
}