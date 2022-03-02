using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public class SyncPlayer : ISyncPlayer
    {
        private readonly IFootballDataApiClient _footballDataApiClient;
        private readonly IPlayerRepository _playerRepository;

        public SyncPlayer(IFootballDataApiClient footballDataApiClient, IPlayerRepository playerRepository)
        {
            _footballDataApiClient = footballDataApiClient;
            _playerRepository = playerRepository;
        }

        public async Task Execute(SynchronizePlayerMatchesContext context)
        {
            var clientPlayer = await _footballDataApiClient.GetPlayerById(context.PlayerId);

            //add player teams (web scrap?)

            Player player = new Player(
                clientPlayer.Id,
                clientPlayer.Name,
                clientPlayer.DateOfBirth,
                clientPlayer.Nationality,
                clientPlayer.Position,
                clientPlayer.Matches.Select(m => new Match(
                    m.Id,
                    context.PlayerId,
                    m.Date,
                    m.HomeTeam,
                    m.AwayTeam,
                    m.HomeTeamScore,
                    m.AwayTeamScore,
                    m.Winner))
                    .ToList());

            var dbPlayer = _playerRepository.GetById(player.ApiId);
            if (dbPlayer == null)
            {
                _playerRepository.Insert(player);
            }
            else
            {
                // _playerRepository.Update(player);
            }

            context.Player = player;


            Console.WriteLine($"Sync player [{player.ApiId}] - SUCCESS");
        }
    }
}
