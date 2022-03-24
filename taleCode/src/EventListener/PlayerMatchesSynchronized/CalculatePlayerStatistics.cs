using Microsoft.Extensions.Hosting;
using SolTechnology.Core.MessageBus.Receive;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;

namespace SolTechnology.TaleCode.EventListener.PlayerMatchesSynchronized
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
