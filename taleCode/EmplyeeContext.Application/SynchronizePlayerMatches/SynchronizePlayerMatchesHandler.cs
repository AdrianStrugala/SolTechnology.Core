using ApiClients;
using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.Domain.Match;
using SolTechnology.TaleCode.Domain.Player;
using SolTechnology.TaleCode.Infrastructure;
using Player = SolTechnology.TaleCode.Domain.Player.Player;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesHandler : ICommandHandler<SynchronizePlayerMatchesCommand>
    {
        private const int SyncCallsLimit = 9;

        private readonly IFootballDataApiClient _footballDataApiClient;
        private readonly IPlayerRepository _playerRepository;
        private readonly IMatchRepository _matchRepository;

        public SynchronizePlayerMatchesHandler(
            IFootballDataApiClient footballDataApiClient,
            IPlayerRepository playerRepository,
            IMatchRepository matchRepository)
        {
            _footballDataApiClient = footballDataApiClient;
            _playerRepository = playerRepository;
            _matchRepository = matchRepository;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {
            Player player = await _footballDataApiClient.GetPlayerById(command.PlayerId);

            _playerRepository.Insert(player);

            var syncedMatches = _matchRepository.GetByPlayerId(player.ApiId);
            var syncedMatchesIds = syncedMatches.Select(m => m.ApiId);

            var matchesToSync = player.Matches.Where(m => !syncedMatchesIds.Contains(m.ApiId)).Take(SyncCallsLimit);

            // sync Match
        }
    }
}
