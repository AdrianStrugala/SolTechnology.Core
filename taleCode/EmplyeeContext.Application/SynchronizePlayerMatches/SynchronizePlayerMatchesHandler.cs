using ApiClients;
using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.Domain.Player;
using SolTechnology.TaleCode.Infrastructure;
using Player = SolTechnology.TaleCode.Domain.Player.Player;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesHandler : ICommandHandler<SynchronizePlayerMatchesCommand>
    {
        private readonly IFootballDataApiClient _footballDataApiClient;
        private readonly IPlayerRepository _playerRepository;

        public SynchronizePlayerMatchesHandler(IFootballDataApiClient footballDataApiClient, IPlayerRepository playerRepository)
        {
            _footballDataApiClient = footballDataApiClient;
            _playerRepository = playerRepository;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {
            Player player = await _footballDataApiClient.GetPlayerById(command.PlayerId);

            _playerRepository.Insert(player);

            // look which matches are not synced

            // get matches competition winners

            // save matches

        }
    }
}
