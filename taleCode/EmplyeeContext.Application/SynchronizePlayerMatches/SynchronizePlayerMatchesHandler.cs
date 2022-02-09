using ApiClients;

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
            var player = await _footballDataApiClient.GetPlayerById(command.PlayerId);

            var x = 1;
        }
    }
}
