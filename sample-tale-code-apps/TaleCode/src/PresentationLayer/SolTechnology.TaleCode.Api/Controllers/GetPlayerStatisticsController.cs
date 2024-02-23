using System.Net;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.Api;
using SolTechnology.Core.CQRS;
using SolTechnology.TaleCode.Api.Examples;
using SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;
using Swashbuckle.AspNetCore.Filters;

namespace SolTechnology.TaleCode.Api.Controllers;

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
    [ProducesResponseType(typeof(ResponseEnvelope<GetPlayerStatisticsResult>), (int)HttpStatusCode.OK),
     SwaggerResponseExample((int)HttpStatusCode.OK, typeof(GetPlayerStatisticsExample))]
    [ProducesResponseType(typeof(ResponseEnvelope<GetPlayerStatisticsResult>), (int)HttpStatusCode.BadRequest),
     SwaggerResponseExample((int)HttpStatusCode.BadRequest, typeof(ErrorExample))]
    public async Task<IActionResult> GetPlayerStatistics(int playerId) =>
        Ok(await _handler.Handle(new GetPlayerStatisticsQuery(playerId)));
}