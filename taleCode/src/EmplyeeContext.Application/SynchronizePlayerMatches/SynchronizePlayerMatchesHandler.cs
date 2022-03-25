using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.StaticData.PlayerId;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesHandler : ICommandHandler<SynchronizePlayerMatchesCommand>
    {
        private readonly ISyncPlayer _syncPlayer;
        private readonly IDetermineMatchesToSync _determineMatchesToSync;
        private readonly ISyncMatch _syncMatch;
        private readonly IPlayerExternalIdsProvider _playerExternalIdsProvider;
        private readonly IMessagePublisher _messagePublisher;

        public SynchronizePlayerMatchesHandler(
            ISyncPlayer syncPlayer,
            IDetermineMatchesToSync determineMatchesToSync,
            ISyncMatch syncMatch,
            IPlayerExternalIdsProvider playerExternalIdsProvider,
            IMessagePublisher messagePublisher)
        {
            _syncPlayer = syncPlayer;
            _determineMatchesToSync = determineMatchesToSync;
            _syncMatch = syncMatch;
            _playerExternalIdsProvider = playerExternalIdsProvider;
            _messagePublisher = messagePublisher;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {
            var playerIdMap = _playerExternalIdsProvider.GetExternalPlayerId(command.PlayerId);
            var context = new SynchronizePlayerMatchesContext
            {
                PlayerIdMap = playerIdMap
            };


            await _syncPlayer.Execute(context);

            _determineMatchesToSync.Execute(context);


            foreach (var matchId in context.MatchesToSync)
            {
                await _syncMatch.Execute(context, matchId);
            }

            var message = new PlayerMatchesSynchronizedEvent(command.PlayerId);
            await _messagePublisher.Publish(message);
        }
    }
}
