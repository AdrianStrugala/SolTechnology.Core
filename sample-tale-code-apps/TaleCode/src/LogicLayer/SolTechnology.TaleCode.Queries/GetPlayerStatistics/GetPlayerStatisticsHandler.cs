using MediatR;
using SolTechnology.Core.CQRS;
using SolTechnology.TaleCode.BlobData.PlayerStatisticsRepository;

namespace SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics
{
    public class GetPlayerStatisticsHandler : IRequestHandler<GetPlayerStatisticsQuery, Result<GetPlayerStatisticsResult>>
    {
        // private readonly IPlayerStatisticsRepository _playerStatisticsRepository;

        public GetPlayerStatisticsHandler()
        // public GetPlayerStatisticsHandler(IPlayerStatisticsRepository playerStatisticsRepository)
        {
            // _playerStatisticsRepository = playerStatisticsRepository;
        }

        public async Task<Result<GetPlayerStatisticsResult>> Handle(GetPlayerStatisticsQuery query, CancellationToken cancellationToken)
        {
            return Result<GetPlayerStatisticsResult>.Success(new GetPlayerStatisticsResult
            {
                Id = 2137
            });

            // var data = await _playerStatisticsRepository.Get(query.PlayerId);
            //
            // var result = new GetPlayerStatisticsResult
            // {
            //     Id = data.Id,
            //     Name = data.Name,
            //     NumberOfMatches = data.NumberOfMatches,
            //     StatisticsByTeams = data.StatisticsByTeams.Select(s => new StatisticsByTeam
            //     {
            //         DateFrom = s.DateFrom,
            //         DateTo = s.DateTo,
            //         NumberOfMatches = s.NumberOfMatches,
            //         PercentageOfMatchesResultingCompetitionVictory = s.PercentageOfMatchesResultingCompetitionVictory,
            //         TeamName = s.TeamName
            //     }).ToList()
            // };
            //
            // return Result<GetPlayerStatisticsResult>.Success(result);
        }
    }
}
