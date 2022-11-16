using SolTechnology.Core.Api;
using SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;
using Swashbuckle.AspNetCore.Filters;

namespace SolTechnology.TaleCode.Api.Examples;

public class ErrorExample : IExamplesProvider<Response<GetPlayerStatisticsResult>>
{
    public Response<GetPlayerStatisticsResult> GetExamples()
    {
        return new Response<GetPlayerStatisticsResult>
        {
            IsSuccess = false,
            Error = "A bug appeared. Might be caterpie :O"
        };
    }
}