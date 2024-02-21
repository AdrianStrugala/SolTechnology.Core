using SolTechnology.Core.Cache;
using SolTechnology.Core.CQRS;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi;
using SolTechnology.TaleCode.ApiClients.FootballDataApi;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;
using SolTechnology.TaleCode.StaticData.PlayerId;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public interface ISyncPlayer
    {
        Task<OperationResult> Execute(SynchronizePlayerMatchesContext context);
    }

    public class SyncPlayer : ISyncPlayer
    {
        private readonly IPlayerExternalIdsProvider _externalIdsProvider;
        private readonly IFootballDataApiClient _footballDataApiClient;
        private readonly IPlayerRepository _playerRepository;
        private readonly IApiFootballApiClient _apiFootballApiClient;
        private readonly ILazyTaskCache _lazyTaskCache;

        public SyncPlayer(
            IPlayerExternalIdsProvider externalIdsProvider,
            IFootballDataApiClient footballDataApiClient,
            IPlayerRepository playerRepository,
            IApiFootballApiClient apiFootballApiClient,
            ILazyTaskCache lazyTaskCache)
        {
            _externalIdsProvider = externalIdsProvider;
            _footballDataApiClient = footballDataApiClient;
            _playerRepository = playerRepository;
            _apiFootballApiClient = apiFootballApiClient;
            _lazyTaskCache = lazyTaskCache;
        }

        public async Task<OperationResult> Execute(SynchronizePlayerMatchesContext context)
        {
            var playerIdMap = _externalIdsProvider.Get(context.PlayerId);
            var clientPlayer = await _lazyTaskCache.GetOrAdd(playerIdMap.FootballDataId, _footballDataApiClient.GetPlayerById);

            var teams = await _apiFootballApiClient.GetPlayerTeams(playerIdMap.ApiFootballId);

            Player player = new Player(
                clientPlayer.Id,
                clientPlayer.Name,
                clientPlayer.DateOfBirth,
                clientPlayer.Nationality,
                clientPlayer.Position,
                clientPlayer.Matches.Select(m => new Match(
                    m.Id,
                    playerIdMap.FootballDataId,
                    m.Date,
                    m.HomeTeam,
                    m.AwayTeam,
                    m.HomeTeamScore,
                    m.AwayTeamScore,
                    m.Winner))
                    .ToList(),
                teams.Select(t => new Team(
                        playerIdMap.FootballDataId,
                    t.TimeFrom,
                    t.TimeTo,
                    t.Name))
                    .ToList());

            var dbPlayer = _playerRepository.GetById(player.ApiId);
            if (dbPlayer == null)
            {
                _playerRepository.Insert(player);
            }
            else
            {
                _playerRepository.Update(player);
            }

            context.Player = player;
            return OperationResult.Succeeded();
        }
    }
}
