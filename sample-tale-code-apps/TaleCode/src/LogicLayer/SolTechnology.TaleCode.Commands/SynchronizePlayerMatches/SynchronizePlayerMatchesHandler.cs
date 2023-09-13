using SolTechnology.Core.CQRS;
using SolTechnology.Core.MessageBus;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.StaticData.PlayerId;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesHandler : ICommandHandler<SynchronizePlayerMatchesCommand>
    {
        private Func<int, PlayerIdMap> GetPlayerId { get; }
        private Func<IMessage, Task> PublishMessage { get; }
        private Func<int, int, Task> SynchronizeMatch { get; }
        private Func<Player, List<int>> CalculateMatchesToSync { get; }
        private Func<PlayerIdMap, Task<Player>> SynchronizePlayer { get; }

        public SynchronizePlayerMatchesHandler(
            ISyncPlayer syncPlayer,
            IDetermineMatchesToSync determineMatchesToSync,
            ISyncMatch syncMatch,
            IPlayerExternalIdsProvider playerExternalIdsProvider,
            IMessagePublisher messagePublisher)
        {
            GetPlayerId = playerExternalIdsProvider.Get;
            SynchronizePlayer = syncPlayer.Execute;
            CalculateMatchesToSync = determineMatchesToSync.Execute;
            SynchronizeMatch = syncMatch.Execute;
            PublishMessage = messagePublisher.Publish;
        }

        public async Task<CommandResult> Handle(SynchronizePlayerMatchesCommand command)
        {
            await Chain
                .Start(() => GetPlayerId(command.PlayerId))
                .Then(SynchronizePlayer)
                .Then(CalculateMatchesToSync)
                .Then(match => match.ForEach(id =>
                    SynchronizeMatch(command.PlayerId, id)))
                .Then(_ => new PlayerMatchesSynchronizedEvent(command.PlayerId))
                .Then(PublishMessage)
                .EndCommand();

            return CommandResult.Succeeded();
        }
    }
}
