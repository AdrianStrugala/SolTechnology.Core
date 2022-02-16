using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public class SyncPlayer : ISyncPlayer
    {
        private readonly IFootballDataApiClient _footballDataApiClient;

        public SyncPlayer(IFootballDataApiClient footballDataApiClient)
        {
            _footballDataApiClient = footballDataApiClient;
        }

        public async Task Execute(SynchronizePlayerMatchesContext context)
        {
            var clientPlayer = await _footballDataApiClient.GetPlayerById(context.Command.PlayerId);

            //add player teams (web scrap?)

            Player player = new Player(
                clientPlayer.Id,
                clientPlayer.Name,
                clientPlayer.DateOfBirth,
                clientPlayer.Nationality,
                clientPlayer.Position,
                new List<Match>());

            context.Player = player;
        }
    }
}
