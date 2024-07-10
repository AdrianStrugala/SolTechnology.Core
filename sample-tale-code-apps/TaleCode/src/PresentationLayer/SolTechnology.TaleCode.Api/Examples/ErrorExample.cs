using SolTechnology.Core.Api;
using SolTechnology.Core.CQRS;
using SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;
using Swashbuckle.AspNetCore.Filters;

namespace SolTechnology.TaleCode.Api.Examples;

public class ErrorExample : IExamplesProvider<Result<GetPlayerStatisticsResult>>
{
    public Result<GetPlayerStatisticsResult> GetExamples()
    {
        return new Result<GetPlayerStatisticsResult>
        {
            IsSuccess = false,
            Error = new Error
            {
                Message = "A bug appeared. Might be caterpie :O"
            }
        };
    }
}