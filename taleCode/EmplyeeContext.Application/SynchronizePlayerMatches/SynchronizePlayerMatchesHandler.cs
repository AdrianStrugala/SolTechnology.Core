using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesHandler : ICommandHandler<SynchronizePlayerMatchesCommand>
    {
        private const int SyncCallsLimit = 9;

        private readonly IPlayerRepository _playerRepository;
        private readonly IBuildPlayer _buildPlayer;
        private readonly IMatchRepository _matchRepository;
        private readonly IAssignWinner _assignWinner;

        public SynchronizePlayerMatchesHandler(
            IBuildPlayer buildPlayer,
            IPlayerRepository playerRepository,
            IMatchRepository matchRepository,
            IAssignWinner assignWinner)
        {
            _buildPlayer = buildPlayer;
            _matchRepository = matchRepository;
            _assignWinner = assignWinner;
            _playerRepository = playerRepository;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {
            var player = await _buildPlayer.Execute(command.PlayerId);

            var dbPlayer = _playerRepository.GetById(player.ApiId);
            if (dbPlayer == null)
            {
                _playerRepository.Insert(player);
            }
            else
            {
                _playerRepository.Update(player);
            }


            var syncedMatches = _matchRepository.GetByPlayerId(command.PlayerId);
            var syncedMatchesIds = syncedMatches.Select(m => m.ApiId);

            var matchesToSync = player.Matches
                                      .Where(m => !syncedMatchesIds.Contains(m.ApiId))
                                      .OrderBy(m => m.Date)
                                      .Take(SyncCallsLimit);


            foreach (var match in matchesToSync)
            {
                await _assignWinner.Execute(match);
            }

            //    _matchRepository.BulkInsert(context.Matches);
        }
    }
}
