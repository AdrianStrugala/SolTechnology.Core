using System.Net;
using MediatR;
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
    private readonly IMediator _mediator;

    public GetPlayerStatisticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("GetPlayerStatistics/{playerId}")]
    [ProducesResponseType(typeof(Result<GetPlayerStatisticsResult>), (int)HttpStatusCode.OK),
     SwaggerResponseExample((int)HttpStatusCode.OK, typeof(GetPlayerStatisticsExample))]
    [ProducesResponseType(typeof(Result<GetPlayerStatisticsResult>), (int)HttpStatusCode.BadRequest),
     SwaggerResponseExample((int)HttpStatusCode.BadRequest, typeof(ErrorExample))]
    public async Task<IActionResult> GetPlayerStatistics(int playerId) =>
        Ok(await _mediator.Send(new GetPlayerStatisticsQuery(playerId)));
}