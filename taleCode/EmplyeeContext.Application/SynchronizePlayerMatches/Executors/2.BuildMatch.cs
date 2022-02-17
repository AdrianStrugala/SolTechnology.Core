using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using Match = SolTechnology.TaleCode.Domain.Match;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public class BuildMatch : IBuildMatch
    {
        private readonly IFootballDataApiClient _footballDataApiClient;

        public BuildMatch(IFootballDataApiClient footballDataApiClient)
        {
            _footballDataApiClient = footballDataApiClient;
        }

        public async Task<Match> Execute(int playerId, int matchId)
        {
            var clientMatch = await _footballDataApiClient.GetMatchById(matchId);

            var match = new Match(
                clientMatch.Id,
                playerId,
                clientMatch.Date,
                clientMatch.HomeTeam,
                clientMatch.AwayTeam,
                clientMatch.HomeTeamScore,
                clientMatch.AwayTeamScore,
                clientMatch.Winner,
                clientMatch.CompetitionWinner);

            return match;
        }
    }
}
