using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public class BuildPlayer : IBuildPlayer
    {
        private readonly IFootballDataApiClient _footballDataApiClient;

        public BuildPlayer(IFootballDataApiClient footballDataApiClient)
        {
            _footballDataApiClient = footballDataApiClient;
        }

        public async Task<Player> Execute(int playerId)
        {
            var clientPlayer = await _footballDataApiClient.GetPlayerById(playerId);

            //add player teams (web scrap?)

            Player player = new Player(
                clientPlayer.Id,
                clientPlayer.Name,
                clientPlayer.DateOfBirth,
                clientPlayer.Nationality,
                clientPlayer.Position,
                new List<Match>());

            return player;
        }
    }
}
