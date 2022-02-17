using ApiClients;
using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesHandler : ICommandHandler<SynchronizePlayerMatchesCommand>
    {
        private const int SyncCallsLimit = 9;

        private readonly IFootballDataApiClient _footballDataApiClient;
        private readonly IPlayerRepository _playerRepository;
        private readonly IBuildPlayer _buildPlayer;
        private readonly IMatchRepository _matchRepository;
        private readonly IBuildMatch _buildMatch;

        public SynchronizePlayerMatchesHandler(
            IBuildPlayer buildPlayer,
            IPlayerRepository playerRepository,
            IMatchRepository matchRepository,
            IBuildMatch buildMatch)
        {
            _buildPlayer = buildPlayer;
            _matchRepository = matchRepository;
            _buildMatch = buildMatch;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {

            var player = await _buildPlayer.Execute(command.PlayerId);
            //   _playerRepository.AddOrUpdate(context.Player);


            var syncedMatches = _matchRepository.GetByPlayerId(command.PlayerId);
            var syncedMatchesIds = syncedMatches.Select(m => m.ApiId);

            var matchesToSync = player.Matches
                                      .Where(m => !syncedMatchesIds.Contains(m.ApiId))
                                      .OrderBy(m => m.Date)
                                      .Take(SyncCallsLimit);

            foreach (var match in matchesToSync)
            {
                await _buildMatch.Execute(player.ApiId, match.ApiId);
            }

            //    _matchRepository.BulkInsert(context.Matches);
        }
    }
}
