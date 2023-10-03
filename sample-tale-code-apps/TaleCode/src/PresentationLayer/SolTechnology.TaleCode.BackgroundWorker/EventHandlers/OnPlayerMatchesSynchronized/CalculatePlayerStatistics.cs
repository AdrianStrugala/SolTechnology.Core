using SolTechnology.Core.CQRS;
using SolTechnology.Core.MessageBus.Receive;
using SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;

namespace SolTechnology.TaleCode.BackgroundWorker.EventHandlers.OnPlayerMatchesSynchronized
{
    public class CalculatePlayerStatistics : IMessageHandler<PlayerMatchesSynchronizedEvent>
    {
        private readonly ICommandHandler<CalculatePlayerStatisticsCommand> _handler;

        public CalculatePlayerStatistics(ICommandHandler<CalculatePlayerStatisticsCommand> handler)
        {
            _handler = handler;
        }

        public async Task Handle(PlayerMatchesSynchronizedEvent message, CancellationToken cancellationToken)
        {
            var command = new CalculatePlayerStatisticsCommand(message.PlayerId);
            await _handler.Handle(command);
        }

    }
}
