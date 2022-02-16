using ApiClients.FootballDataApi;
using Match = SolTechnology.TaleCode.Domain.Match;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public class SyncMatch
    {
        private readonly IFootballDataApiClient _footballDataApiClient;

        public SyncMatch(IFootballDataApiClient footballDataApiClient)
        {
            _footballDataApiClient = footballDataApiClient;
        }

        public async Task Execute(int matchId, int playerId)
        {
            var clientMatch = await _footballDataApiClient.GetMatchById(matchId);

            // var match = new Match(
            //     clientMatch.Id,
            //     playerId,
            //     clientMatch.Date,
            //     clientMatch.HomeTeam,
            //     clientMatch.AwayTeam,
            //     clientMatch.HomeTeamScore,
            //     clientMatch.AwayTeamScore,
            //     clientMatch.Winner,
            //     clientMatch.CompetitionWinner);
        }
    }
}
