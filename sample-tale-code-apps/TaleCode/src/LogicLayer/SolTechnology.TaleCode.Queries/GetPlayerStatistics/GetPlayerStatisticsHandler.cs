using SolTechnology.Core.CQRS;
using SolTechnology.TaleCode.BlobData.PlayerStatisticsRepository;

namespace SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics
{
    public class GetPlayerStatisticsHandler : IQueryHandler<GetPlayerStatisticsQuery, GetPlayerStatisticsResult>
    {
        private readonly IPlayerStatisticsRepository _playerStatisticsRepository;

        public GetPlayerStatisticsHandler(IPlayerStatisticsRepository playerStatisticsRepository)
        {
            _playerStatisticsRepository = playerStatisticsRepository;
        }

        public async Task<OperationResult<GetPlayerStatisticsResult>> Handle(GetPlayerStatisticsQuery query, CancellationToken cancellationToken)
        {
            return OperationResult<GetPlayerStatisticsResult>.Succeeded(new GetPlayerStatisticsResult
            {
                Id = 2137
            });

            var data = await _playerStatisticsRepository.Get(query.PlayerId);

            var result = new GetPlayerStatisticsResult
            {
                Id = data.Id,
                Name = data.Name,
                NumberOfMatches = data.NumberOfMatches,
                StatisticsByTeams = data.StatisticsByTeams.Select(s => new StatisticsByTeam
                {
                    DateFrom = s.DateFrom,
                    DateTo = s.DateTo,
                    NumberOfMatches = s.NumberOfMatches,
                    PercentageOfMatchesResultingCompetitionVictory = s.PercentageOfMatchesResultingCompetitionVictory,
                    TeamName = s.TeamName
                }).ToList()
            };

            return OperationResult<GetPlayerStatisticsResult>.Succeeded(result);
        }
    }
}
