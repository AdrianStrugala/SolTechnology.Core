using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesHandler : ICommandHandler<SynchronizePlayerMatchesCommand>
    {
        private readonly ISyncPlayer _syncPlayer;
        private readonly IDetermineMatchesToSync _determineMatchesToSync;
        private readonly ISyncMatch _syncMatch;
        private readonly ILogger<SynchronizePlayerMatchesHandler> _logger;

        public SynchronizePlayerMatchesHandler(
            ISyncPlayer syncPlayer,
            IDetermineMatchesToSync determineMatchesToSync,
            ISyncMatch syncMatch,
            ILogger<SynchronizePlayerMatchesHandler> logger)
        {
            _syncPlayer = syncPlayer;
            _determineMatchesToSync = determineMatchesToSync;
            _syncMatch = syncMatch;
            _logger = logger;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {
            using (_logger.OperationStarted(nameof(SynchronizePlayerMatches), new { command.PlayerId }))
            {
                var context = new SynchronizePlayerMatchesContext
                {
                    PlayerId = command.PlayerId
                };


                await _syncPlayer.Execute(context);

                _determineMatchesToSync.Execute(context);


                foreach (var matchId in context.MatchesToSync)
                {
                    await _syncMatch.Execute(context, matchId);
                }

                _logger.OperationSucceeded();
            }
        }
    }
}
