using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Logging;
using SolTechnology.TaleCode.ApiClients.FootballDataApi;
using SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;
using Match = SolTechnology.TaleCode.Domain.Match;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public interface ISyncMatches
    {
        Task<OperationResult> Execute(SynchronizePlayerMatchesContext context);
    }

    public class SyncMatches : ISyncMatches
    {
        private readonly IFootballDataApiClient _footballDataApiClient;
        private readonly IMatchRepository _matchRepository;
        private readonly IExecutionErrorRepository _executionErrorRepository;
        private readonly ILogger<SyncMatches> _logger;

        public SyncMatches(
            IFootballDataApiClient footballDataApiClient,
            IMatchRepository matchRepository,
            IExecutionErrorRepository executionErrorRepository,
            ILogger<SyncMatches> logger)
        {
            _footballDataApiClient = footballDataApiClient;
            _matchRepository = matchRepository;
            _executionErrorRepository = executionErrorRepository;
            _logger = logger;
        }

        public async Task<OperationResult> Execute(SynchronizePlayerMatchesContext context)
        {
            List<Task> tasks = new List<Task>();
            context.MatchesToSync.ForEach(matchId => tasks.Add(SyncMatch(context.PlayerId, matchId)));
            await Task.WhenAll(tasks);

            return OperationResult.Succeeded();
        }

        private async Task SyncMatch(int playerId, int matchId)
        {
            using (_logger.BeginOperationScope(new { MatchId = matchId }))
            {
                _logger.OperationStarted(nameof(SyncMatches));

                var clientMatch = await _footballDataApiClient.GetMatchById(matchId);

                try
                {
                    Match match = new Match(
                        clientMatch.Id,
                        playerId,
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
                    _executionErrorRepository.Insert(new ExecutionError(ReferenceType.Match, clientMatch.Id,
                        e.Message));
                }
            }
        }
    }
}
