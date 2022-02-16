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
        private readonly ISyncPlayer _syncPlayer;
        private readonly IMatchRepository _matchRepository;

        public SynchronizePlayerMatchesHandler(
            ISyncPlayer syncPlayer,
            IPlayerRepository playerRepository,
            IMatchRepository matchRepository)
        {
            _syncPlayer = syncPlayer;
            _matchRepository = matchRepository;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {
            var context = new SynchronizePlayerMatchesContext
            {
                Command = command
            };

            await _syncPlayer.Execute(context);
            //   _playerRepository.AddOrUpdate(context.Player);


            var syncedMatches = _matchRepository.GetByPlayerId(command.PlayerId);
            var syncedMatchesIds = syncedMatches.Select(m => m.ApiId);

            var matchesToSync = context.Player.Matches
                                      .Where(m => !syncedMatchesIds.Contains(m.ApiId))
                                      .OrderBy(m => m.Date)
                                      .Take(SyncCallsLimit);

            // sync Match
        }
    }
}
