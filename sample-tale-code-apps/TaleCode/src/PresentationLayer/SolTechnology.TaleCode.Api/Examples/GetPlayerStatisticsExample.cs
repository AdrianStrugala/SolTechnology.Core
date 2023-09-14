using SolTechnology.Core.Api;
using SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;
using Swashbuckle.AspNetCore.Filters;

namespace SolTechnology.TaleCode.Api.Examples;

public class GetPlayerStatisticsExample : IExamplesProvider<ResponseEnvelope<GetPlayerStatisticsResult>>
{
    public ResponseEnvelope<GetPlayerStatisticsResult> GetExamples()
    {
        return new ResponseEnvelope<GetPlayerStatisticsResult>
        {
            IsSuccess = true,
            Data = new GetPlayerStatisticsResult
            {
                Id = 2137,
                Name = "Cristiano Ronaldo",
                NumberOfMatches = 240395,
                StatisticsByTeams = new List<StatisticsByTeam>
                {
                    new StatisticsByTeam
                    {
                        DateFrom = DateTime.Now.AddDays(-304),
                        DateTo = DateTime.Now,
                        NumberOfMatches = 45,
                        TeamName = "Manchester United",
                        PercentageOfMatchesResultingCompetitionVictory = 100
                    }
                }
            }
        };
    }
}