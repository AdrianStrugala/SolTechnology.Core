using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using SolTechnology.TaleCode.Infrastructure;
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
        private readonly ILogger<SynchronizePlayerMatchesHandler> _logger;

        public SynchronizePlayerMatchesHandler(
            ISyncPlayer syncPlayer,
            IDetermineMatchesToSync determineMatchesToSync,
            ISyncMatch syncMatch,
            IPlayerIdProvider playerIdProvider,
            ILogger<SynchronizePlayerMatchesHandler> logger)
        {
            _syncPlayer = syncPlayer;
            _determineMatchesToSync = determineMatchesToSync;
            _syncMatch = syncMatch;
            _playerIdProvider = playerIdProvider;
            _logger = logger;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {
            using (_logger.BeginOperationScope(new { PlayerName = command.PlayerName }))
            {
                _logger.OperationStarted(nameof(SynchronizePlayerMatches));

                try
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


                    //TODO: Calculate Player STATISTICS BITCH

                    _logger.OperationSucceeded(nameof(SynchronizePlayerMatches));
                }
                catch (Exception e)
                {
                    _logger.OperationFailed(nameof(SyncMatch), e);
                    throw;
                }

            }
        }
    }
}
