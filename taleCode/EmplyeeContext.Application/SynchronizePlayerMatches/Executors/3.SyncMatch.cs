using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;
using Match = SolTechnology.TaleCode.Domain.Match;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public class SyncMatch : ISyncMatch
    {
        private readonly IFootballDataApiClient _footballDataApiClient;
        private readonly IMatchRepository _matchRepository;

        public SyncMatch(IFootballDataApiClient footballDataApiClient, IMatchRepository matchRepository)
        {
            _footballDataApiClient = footballDataApiClient;
            _matchRepository = matchRepository;
        }

        public async Task Execute(SynchronizePlayerMatchesContext context, int matchId)
        {
            try
            {
                var clientMatch = await _footballDataApiClient.GetMatchById(matchId);

                Match match = new Match(
                    clientMatch.Id,
                    context.PlayerId,
                    clientMatch.Date,
                    clientMatch.HomeTeam,
                    clientMatch.AwayTeam,
                    clientMatch.HomeTeamScore,
                    clientMatch.AwayTeamScore,
                    clientMatch.Winner);

                match.AssignCompetitionWinner(clientMatch.CompetitionWinner);

                _matchRepository.Insert(match);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
