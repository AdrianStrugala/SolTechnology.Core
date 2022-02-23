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

        public SynchronizePlayerMatchesHandler(
            ISyncPlayer syncPlayer,
            IDetermineMatchesToSync determineMatchesToSync,
            ISyncMatch syncMatch)
        {
            _syncPlayer = syncPlayer;
            _determineMatchesToSync = determineMatchesToSync;
            _syncMatch = syncMatch;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
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
        }
    }
}
