using SolTechnology.TaleCode.ApiClients.FootballDataApi;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public class SyncPlayer : ISyncPlayer
    {
        private readonly IFootballDataApiClient _footballDataApiClient;
        private readonly IPlayerRepository _playerRepository;
        private readonly IApiFootballApiClient _apiFootballApiClient;

        public SyncPlayer(IFootballDataApiClient footballDataApiClient, IPlayerRepository playerRepository, IApiFootballApiClient apiFootballApiClient)
        {
            _footballDataApiClient = footballDataApiClient;
            _playerRepository = playerRepository;
            _apiFootballApiClient = apiFootballApiClient;
        }

        public async Task Execute(SynchronizePlayerMatchesContext context)
        {
            var clientPlayer = await _footballDataApiClient.GetPlayerById(context.PlayerIdMap.FootballDataId);

            var teams = await _apiFootballApiClient.GetPlayerTeams(context.PlayerIdMap.ApiFootballId);

            Player player = new Player(
                clientPlayer.Id,
                clientPlayer.Name,
                clientPlayer.DateOfBirth,
                clientPlayer.Nationality,
                clientPlayer.Position,
                clientPlayer.Matches.Select(m => new Match(
                    m.Id,
                    context.PlayerIdMap.FootballDataId,
                    m.Date,
                    m.HomeTeam,
                    m.AwayTeam,
                    m.HomeTeamScore,
                    m.AwayTeamScore,
                    m.Winner))
                    .ToList(),
                teams.Select(t => new Team(
                    context.PlayerIdMap.FootballDataId,
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
        }
    }
}
