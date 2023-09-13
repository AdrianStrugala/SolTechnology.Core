using SolTechnology.Core.Api;
using SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;
using Swashbuckle.AspNetCore.Filters;

namespace SolTechnology.TaleCode.Api.Examples;

public class ErrorExample : IExamplesProvider<ResponseEnvelope<GetPlayerStatisticsResult>>
{
    public ResponseEnvelope<GetPlayerStatisticsResult> GetExamples()
    {
        return new ResponseEnvelope<GetPlayerStatisticsResult>
        {
            IsSuccess = false,
            Error = "A bug appeared. Might be caterpie :O"
        };
    }
}