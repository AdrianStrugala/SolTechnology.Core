using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using SolTechnology.Core.MessageBus;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.StaticData.PlayerId;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesHandler : ICommandHandler<SynchronizePlayerMatchesCommand>
    {
        private readonly ISyncPlayer _syncPlayer;
        private readonly IDetermineMatchesToSync _determineMatchesToSync;
        private readonly ISyncMatch _syncMatch;
        private readonly IPlayerIdProvider _playerIdProvider;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<SynchronizePlayerMatchesHandler> _logger;
        private readonly ICommandHandler<CalculatePlayerStatisticsCommand> _calculatePlayerStatsHandler;

        public SynchronizePlayerMatchesHandler(
            ISyncPlayer syncPlayer,
            IDetermineMatchesToSync determineMatchesToSync,
            ISyncMatch syncMatch,
            IPlayerIdProvider playerIdProvider,
            IMessagePublisher messagePublisher,
            ILogger<SynchronizePlayerMatchesHandler> logger,


            //TEMPORARY:
            ICommandHandler<CalculatePlayerStatisticsCommand> calculatePlayerStatsHandler)
        {
            _syncPlayer = syncPlayer;
            _determineMatchesToSync = determineMatchesToSync;
            _syncMatch = syncMatch;
            _playerIdProvider = playerIdProvider;
            _messagePublisher = messagePublisher;
            _logger = logger;
            _calculatePlayerStatsHandler = calculatePlayerStatsHandler;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {

            var playerIdMap = _playerIdProvider.GetPlayerId(command.PlayerName);
            var context = new SynchronizePlayerMatchesContext
            {
                PlayerName = command.PlayerName,
                PlayerIdMap = playerIdMap
            };


            await _syncPlayer.Execute(context);

            _determineMatchesToSync.Execute(context);


            foreach (var matchId in context.MatchesToSync)
            {
                await _syncMatch.Execute(context, matchId);
            }






            // TODO: TEMP Calculate Player STATISTICS
            await _calculatePlayerStatsHandler.Handle(new CalculatePlayerStatisticsCommand
            { PlayerName = command.PlayerName });


            var message = new PlayerMatchesSynchronizedEvent(command.PlayerName);
            await _messagePublisher.Publish(message);

        }
    }
}
