using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using SolTechnology.TaleCode.ApiClients.FootballDataApi;
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
        private readonly ILogger<SyncMatch> _logger;

        public SyncMatch(
            IFootballDataApiClient footballDataApiClient,
            IMatchRepository matchRepository,
            IExecutionErrorRepository executionErrorRepository,
            ILogger<SyncMatch> logger)
        {
            _footballDataApiClient = footballDataApiClient;
            _matchRepository = matchRepository;
            _executionErrorRepository = executionErrorRepository;
            _logger = logger;
        }

        public async Task Execute(SynchronizePlayerMatchesContext context, int matchId)
        {
            using (_logger.OperationStarted(nameof(SyncMatch), new { matchId }))
            {
                var clientMatch = await _footballDataApiClient.GetMatchById(matchId);

                try
                {
                    Match match = new Match(
                        clientMatch.Id,
                        context.PlayerIdMap.FootballDataId,
                        clientMatch.Date,
                        clientMatch.HomeTeam,
                        clientMatch.AwayTeam,
                        clientMatch.HomeTeamScore,
                        clientMatch.AwayTeamScore,
                        clientMatch.Winner);

                    match.AssignCompetitionWinner(clientMatch.CompetitionWinner);

                    _matchRepository.Insert(match);

                    _logger.OperationSucceeded(nameof(SyncMatch));
                }
                catch (Exception e)
                {
                    _logger.OperationFailed(nameof(SyncMatch), e);

                    _executionErrorRepository.Insert(new ExecutionError(ReferenceType.Match, clientMatch.Id,
                        e.Message));
                }
            }
        }
    }
}
