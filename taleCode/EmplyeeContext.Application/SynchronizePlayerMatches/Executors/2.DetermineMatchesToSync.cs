using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public class DetermineMatchesToSync : IDetermineMatchesToSync
    {
        private const int SyncCallsLimit = 9;

        private readonly IMatchRepository _matchRepository;

        public DetermineMatchesToSync(IMatchRepository matchRepository)
        {
            _matchRepository = matchRepository;
        }

        public void Execute(SynchronizePlayerMatchesContext context)
        {
            var syncedMatches = _matchRepository.GetByPlayerId(context.PlayerId);
            var syncedMatchesIds = syncedMatches.Select(m => m.ApiId);

            var matchesToSync = context.Player.Matches
                .Where(m => !syncedMatchesIds.Contains(m.ApiId))
                .OrderBy(m => m.Date)
                .Take(SyncCallsLimit)
                .Select(m => m.ApiId)
                .ToList();

            context.MatchesToSync = matchesToSync;
        }
    }
}
