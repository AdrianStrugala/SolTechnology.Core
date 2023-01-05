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
        private readonly ISyncPlayer _syncPlayer;
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
            _syncPlayer = syncPlayer;
            CalculateMatchesToSync = determineMatchesToSync.Execute;
            SynchronizeMatch = syncMatch.Execute;
            PublishMessage = messagePublisher.Publish;
        }

        public async Task Handle(SynchronizePlayerMatchesCommand command)
        {
            try
            {
                var map = GetPlayerId(command.PlayerId);
                var xd = await _syncPlayer.Execute(map);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
         

            // try
            // {
            //     await Chain
            //         .Start(() => GetPlayerId(command.PlayerId))
            //         .Then(SynchronizePlayer)
            //         .Then(CalculateMatchesToSync)
            //         .Then(match => match.ForEach(id =>
            //             SynchronizeMatch(id, command.PlayerId)))
            //         .Then(_ => new PlayerMatchesSynchronizedEvent(command.PlayerId))
            //         .Then(PublishMessage)
            //         .EndCommand();
            // }
            // catch (Exception e)
            // {
            //     var x = e;
            //     throw;
            // }
        }
    }
}
