using ApiClients;
using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.Infrastructure;
using Player = SolTechnology.TaleCode.Domain.Player;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesHandler : ICommandHandler<SynchronizePlayerMatchesCommand>
    {
        private readonly IFootballDataApiClient _footballDataApiClient;

        public SynchronizePlayerMatchesHandler(IFootballDataApiClient footballDataApiClient)
        {
            _footballDataApiClient = footballDataApiClient;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {
            Player player = await _footballDataApiClient.GetPlayerById(command.PlayerId);

            // repository.StorePlayer();

            // look which matches are not synced

            // get matches competition winners

            // save matches

        }
    }
}
