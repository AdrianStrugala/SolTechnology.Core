using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;
using Match = SolTechnology.TaleCode.Domain.Match;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public class SyncMatch : ISyncMatch
    {
        private readonly IFootballDataApiClient _footballDataApiClient;
        private readonly IMatchRepository _matchRepository;
        private readonly IExecutionErrorRepository _executionErrorRepository;

        public SyncMatch(IFootballDataApiClient footballDataApiClient, IMatchRepository matchRepository, IExecutionErrorRepository executionErrorRepository)
        {
            _footballDataApiClient = footballDataApiClient;
            _matchRepository = matchRepository;
            _executionErrorRepository = executionErrorRepository;
        }

        public async Task Execute(SynchronizePlayerMatchesContext context, int matchId)
        {
            var clientMatch = await _footballDataApiClient.GetMatchById(matchId);

            try
            {
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

                Console.WriteLine($"Sync match [{match.ApiId}] - SUCCESS");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                _executionErrorRepository.Insert(new ExecutionError(ReferenceType.Match, clientMatch.Id, e.Message));
            }
        }
    }
}
