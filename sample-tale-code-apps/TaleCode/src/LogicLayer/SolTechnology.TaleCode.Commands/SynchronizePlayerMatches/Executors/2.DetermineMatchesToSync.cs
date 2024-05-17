using SolTechnology.Core.CQRS;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public interface IDetermineMatchesToSync
    {
        Task<Result> Execute(SynchronizePlayerMatchesContext context);
    }

    public class DetermineMatchesToSync : IDetermineMatchesToSync
    {
        private const int SyncCallsLimit = 9;

        private readonly IMatchRepository _matchRepository;
        private readonly IExecutionErrorRepository _executionErrorRepository;

        public DetermineMatchesToSync(IMatchRepository matchRepository, IExecutionErrorRepository executionErrorRepository)
        {
            _matchRepository = matchRepository;
            _executionErrorRepository = executionErrorRepository;
        }

        public Task<Result> Execute(SynchronizePlayerMatchesContext context)
        {
            var player = context.Player;
            var syncedMatches = _matchRepository.GetByPlayerId(player.ApiId);
            var syncedMatchesIds = syncedMatches.Select(m => m.ApiId);

            var failedMatches = _executionErrorRepository.GetByReferenceType(ReferenceType.Match);
            var failedMatchesIds = failedMatches.Select(e => e.ReferenceId);

            var matchesToSync = player.Matches
                .Where(m => !syncedMatchesIds.Contains(m.ApiId))
                .Where(m => !failedMatchesIds.Contains(m.ApiId))
                .OrderBy(m => m.Date)
                .Take(SyncCallsLimit)
                .Select(m => m.ApiId)
                .ToList();

            if (matchesToSync.Count == 0)
            {
                Random random = new Random();

                matchesToSync = failedMatchesIds
                    .OrderBy(x => random.Next())
                    .Take(SyncCallsLimit)
                    .ToList();
            }

            context.MatchesToSync = matchesToSync;
            return Result.SuccessAsTask();
        }
    }
}
