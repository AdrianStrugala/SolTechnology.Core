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
        Task<Result> Execute(SynchronizePlayerMatchesContext context);
    }

    public class SyncPlayer : ISyncPlayer
    {
        private readonly IPlayerExternalIdsProvider _externalIdsProvider;
        private readonly IFootballDataApiClient _footballDataApiClient;
        private readonly IPlayerRepository _playerRepository;
        private readonly IApiFootballApiClient _apiFootballApiClient;
        private readonly ISingletonCache _singletonCache;

        public SyncPlayer(
            IPlayerExternalIdsProvider externalIdsProvider,
            IFootballDataApiClient footballDataApiClient,
            IPlayerRepository playerRepository,
            IApiFootballApiClient apiFootballApiClient,
            ISingletonCache singletonCache)
        {
            _externalIdsProvider = externalIdsProvider;
            _footballDataApiClient = footballDataApiClient;
            _playerRepository = playerRepository;
            _apiFootballApiClient = apiFootballApiClient;
            _singletonCache = singletonCache;
        }

        public async Task<Result> Execute(SynchronizePlayerMatchesContext context)
        {
            var playerIdMap = _externalIdsProvider.Get(context.PlayerId);
            var clientPlayer = await _singletonCache.GetOrAdd(playerIdMap.FootballDataId, _footballDataApiClient.GetPlayerById);

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
            return Result.Success();
        }
    }
}
