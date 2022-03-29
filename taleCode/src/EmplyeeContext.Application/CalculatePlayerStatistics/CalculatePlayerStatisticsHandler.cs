using Microsoft.Extensions.Logging;
using SolTechnology.TaleCode.BlobData.PlayerStatisticsRepository;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;
using SolTechnology.TaleCode.StaticData;
using SolTechnology.TaleCode.StaticData.PlayerId;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics
{
    public class CalculatePlayerStatisticsHandler : ICommandHandler<CalculatePlayerStatisticsCommand>
    {
        private readonly IPlayerExternalIdsProvider _playerExternalIdsProvider;
        private readonly IMatchRepository _matchRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IPlayerStatisticsRepository _playerStatisticsRepository;

        public CalculatePlayerStatisticsHandler(
            IPlayerExternalIdsProvider playerExternalIdsProvider,
            IMatchRepository matchRepository,
            IPlayerRepository playerRepository,
            IPlayerStatisticsRepository playerStatisticsRepository)
        {
            _playerExternalIdsProvider = playerExternalIdsProvider;
            _matchRepository = matchRepository;
            _playerRepository = playerRepository;
            _playerStatisticsRepository = playerStatisticsRepository;
        }

        public async Task Handle(CalculatePlayerStatisticsCommand command)
        {
            var result = new PlayerStatistics
            {
                Id = command.PlayerId
            };

            var playerIdMap = _playerExternalIdsProvider.GetExternalPlayerId(command.PlayerId);
            var player = _playerRepository.GetById(playerIdMap.FootballDataId);
            var matches = _matchRepository.GetByPlayerId(playerIdMap.FootballDataId);

            result.Name = player.Name;
            result.NumberOfMatches = matches.Count;

            var nationalTeamMatches = matches
                .Where(m => m.AwayTeam == player.Nationality || m.HomeTeam == player.Nationality).ToList();
            var clubMatches = matches.Except(nationalTeamMatches).ToList();

            result.StatisticsByTeams.Add(
                CalculateSingleTeamStatistics(
                    nationalTeamMatches,
                    new Team(
                        playerIdMap.FootballDataId, DateProvider.DateMin(), DateProvider.DateMax(), player.Nationality)
                    ));

            foreach (var team in player.Teams)
            {
                var teamMatches = clubMatches
                    .Where(m => m.Date > team.DateFrom && m.Date < team.DateTo)
                    .ToList();

                result.StatisticsByTeams.Add(CalculateSingleTeamStatistics(teamMatches, team));
            }

            await _playerStatisticsRepository.Add(result);
        }

        private StatisticsByTeam CalculateSingleTeamStatistics(List<Match> teamMatches, Team team)
        {
            var statistic = new StatisticsByTeam
            {
                TeamName = team.Name,
                DateFrom = team.DateFrom,
                DateTo = team.DateTo,
                NumberOfMatches = teamMatches.Count
            };

            var numberOfMatchesResultingCompetitionVictory = teamMatches.Count(m => m.CompetitionWinner == team.Name);
            if (statistic.NumberOfMatches == 0)
            {
                statistic.PercentageOfMatchesResultingCompetitionVictory = 0;
            }
            else
            {
                statistic.PercentageOfMatchesResultingCompetitionVictory =
                    (numberOfMatchesResultingCompetitionVictory / statistic.NumberOfMatches) * 100;
            }

            return statistic;
        }
    }
}
