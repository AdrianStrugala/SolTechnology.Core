using MediatR;
using SolTechnology.Core.CQRS;
using SolTechnology.TaleCode.BlobData.PlayerStatisticsRepository;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;
using SolTechnology.TaleCode.StaticData;
using SolTechnology.TaleCode.StaticData.PlayerId;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics
{
    public class CalculatePlayerStatisticsHandler : ICommandHandler<CalculatePlayerStatisticsCommand>, IRequestHandler<CalculatePlayerStatisticsCommand, Result>
    {
        private Func<PlayerStatistics, Task> StoreResult { get; }
        private Func<int, List<Match>> GetMatches { get; }
        private Func<int, Player> GetPlayer { get; }
        private Func<int, PlayerIdMap> GetPlayerId { get; }

        public CalculatePlayerStatisticsHandler(
            IPlayerExternalIdsProvider playerExternalIdsProvider,
            IMatchRepository matchRepository,
            IPlayerRepository playerRepository,
            IPlayerStatisticsRepository playerStatisticsRepository)
        {
            GetPlayerId = playerExternalIdsProvider.Get;
            GetMatches = matchRepository.GetByPlayerId;
            GetPlayer = playerRepository.GetById;
            StoreResult = playerStatisticsRepository.Add;
        }

        //Example of ugly implementation even when the standard is in place
        public async Task<Result> Handle(CalculatePlayerStatisticsCommand command, CancellationToken cancellationToken)
        {
            var context = new CalculatePlayerStatisticsContext
            {
                Result = new PlayerStatistics { Id = command.PlayerId }
            };

            return await Chain.Start(context, cancellationToken)
                .Then(ctx => ctx.PlayerIdMap = GetPlayerId(command.PlayerId))
                .Then(ctx => ctx.Player = GetPlayer(ctx.PlayerIdMap.FootballDataId))
                .Then(ctx => ctx.Matches = GetMatches(ctx.PlayerIdMap.FootballDataId))
                .Then(ctx => ctx.NationalTeamMatches = ExtractNationalTeamMatches(ctx.Matches, ctx.Player))
                .Then(ctx => AssignNationalTeamMatches(ctx.Result, ctx.NationalTeamMatches, ctx.Player))
                .Then(ctx => ctx.ClubMatches = ExtractClubMatches(ctx.NationalTeamMatches, ctx.Matches))
                .Then(ctx => AssignClubMatches(ctx.Result, ctx.ClubMatches, ctx.Player))
                .Then(ctx => ApplyPlayerMetadata(ctx.Result, ctx.Player, ctx.Matches))
                .Then(async ctx => await StoreResult(ctx.Result))
                .End(ctx => ctx.Result);
        }


        private List<Match> ExtractNationalTeamMatches(List<Match> allMatches, Player player)
        {
            return allMatches
                .Where(m => m.AwayTeam == player.Nationality ||
                            m.HomeTeam == player.Nationality)
                .ToList();
        }

        private List<Match> AssignNationalTeamMatches(PlayerStatistics result, List<Match> nationalTeamMatches, Player player)
        {
            result.StatisticsByTeams.Add(
                CalculateSingleTeamStatistics(
                    nationalTeamMatches,
                    new Team(
                        player.ApiId, DateProvider.DateMin(), DateProvider.DateMax(), player.Nationality)
                ));

            return nationalTeamMatches;
        }

        private List<Match> ExtractClubMatches(List<Match> nationalTeamMatches, List<Match> matches)
        {
            return matches.Except(nationalTeamMatches).ToList();
        }

        private void AssignClubMatches(PlayerStatistics result, List<Match> clubMatches, Player player)
        {
            foreach (var team in player.Teams)
            {
                var teamMatches = clubMatches
                    .Where(m => m.Date > team.DateFrom && m.Date < team.DateTo)
                    .ToList();

                result.StatisticsByTeams.Add(CalculateSingleTeamStatistics(teamMatches, team));
            }
        }

        private void ApplyPlayerMetadata(PlayerStatistics result, Player player, List<Match> matches)
        {
            result.Name = player.Name;
            result.NumberOfMatches = matches.Count;
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
